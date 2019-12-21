using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	using DefaultHistoryContent = Dictionary<IIdentifier, IReadOnlyList<IExecEvaluator>>;

	public class StateMachineInterpreter
	{
		private const string StateStorageKey                  = "state";
		private const string StateMachineDefinitionStorageKey = "smd";

		private readonly CancellationToken                     _anyToken;
		private readonly DataModelValue                        _arguments;
		private readonly ICollection<ICustomActionProvider>    _customActionProviders;
		private readonly ICollection<IDataModelHandlerFactory> _dataModelHandlerFactories;
		private readonly CancellationToken                     _destroyToken;
		private readonly ChannelReader<IEvent>                 _eventChannel;
		private readonly ExternalCommunicationWrapper          _externalCommunication;
		private readonly LoggerWrapper                         _logger;
		private readonly INotifyStateChanged                   _notifyStateChanged;
		private readonly PersistenceLevel                      _persistenceLevel;
		private readonly IResourceLoader                       _resourceLoader;
		private readonly string                                _sessionId;
		private readonly CancellationToken                     _stopToken;
		private readonly IStorageProvider                      _storageProvider;
		private readonly CancellationToken                     _suspendToken;
		private          IStateMachineContext                  _context;
		private          IDataModelHandler                     _dataModelHandler;
		private          DataModelValue                        _doneData;
		private          InterpreterModel                      _model;
		private          bool                                  _stop;

		private StateMachineInterpreter(string sessionId, ChannelReader<IEvent> eventChannel, in InterpreterOptions options)
		{
			_sessionId = sessionId;
			_eventChannel = eventChannel;
			_suspendToken = options.SuspendToken;
			_stopToken = options.StopToken;
			_destroyToken = options.DestroyToken;
			_anyToken = CancellationTokenSource.CreateLinkedTokenSource(_suspendToken, _destroyToken, _stopToken).Token;
			_resourceLoader = options.ResourceLoader ?? DefaultResourceLoader.Instance;
			_customActionProviders = options.CustomActionProviders;
			_dataModelHandlerFactories = options.DataModelHandlerFactories;
			_logger = new LoggerWrapper(options.Logger ?? DefaultLogger.Instance, sessionId);
			_externalCommunication = new ExternalCommunicationWrapper(options.ExternalCommunication, sessionId);
			_storageProvider = options.StorageProvider ?? NullStorageProvider.Instance;
			_persistenceLevel = options.PersistenceLevel;
			_notifyStateChanged = options.NotifyStateChanged;
			_arguments = options.Arguments.DeepClone(true);
		}

		private bool IsPersistingEnabled => _persistenceLevel != PersistenceLevel.None;

		private bool Running
		{
			get => !_stop && (!IsPersistingEnabled || _context.PersistenceContext.GetState((int) StateBagKey.Stop) == 0);
			set
			{
				_stop = !value;

				if (IsPersistingEnabled)
				{
					_context.PersistenceContext.SetState((int) StateBagKey.Stop, value ? 0 : 1);
				}
			}
		}

		public static ValueTask<StateMachineResult> RunAsync(string sessionId, IStateMachine stateMachine, ChannelReader<IEvent> eventChannel, in InterpreterOptions options = default)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
			if (eventChannel == null) throw new ArgumentNullException(nameof(eventChannel));

			return new StateMachineInterpreter(sessionId, eventChannel, options).Run(stateMachine);
		}

		private async ValueTask<InterpreterModel> BuildInterpreterModel(IStateMachine stateMachine)
		{
			var interpreterModel = IsPersistingEnabled ? await TryRestoreInterpreterModel(stateMachine).ConfigureAwait(false) : null;

			if (interpreterModel != null)
			{
				return interpreterModel;
			}

			var interpreterModelBuilder = new InterpreterModelBuilder();
			var dataModelHandlerFactory = GetDataModelHandlerFactory(stateMachine.DataModelType, _dataModelHandlerFactories);
			_dataModelHandler = dataModelHandlerFactory.CreateHandler(interpreterModelBuilder);

			interpreterModel = await interpreterModelBuilder.Build(stateMachine, _dataModelHandler, _customActionProviders, _resourceLoader, _stopToken).ConfigureAwait(false);

			if (IsPersistingEnabled)
			{
				await SaveInterpreterModel(interpreterModel).ConfigureAwait(false);
			}

			return interpreterModel;
		}

		private async ValueTask<InterpreterModel> TryRestoreInterpreterModel(IStateMachine stateMachine)
		{
			var storage = await _storageProvider.GetTransactionalStorage(partition: default, StateMachineDefinitionStorageKey, _stopToken).ConfigureAwait(false);
			await using (storage.ConfigureAwait(false))
			{
				var bucket = new Bucket(storage);

				if (bucket.TryGet(Key.Version, out int version) && version != 1)
				{
					throw new InvalidOperationException("Persisted state can't be read. Unsupported version.");
				}

				if (bucket.TryGet(Key.SessionId, out string sessionId) && sessionId != _sessionId)
				{
					throw new InvalidOperationException("Persisted state can't be read. Stored and provided SessionIds does not match.");
				}

				if (!bucket.TryGet(Key.StateMachineDefinition, out var memory))
				{
					return null;
				}

				var smdBucket = new Bucket(new InMemoryStorage(memory.Span));

				var interpreterModelBuilder = new InterpreterModelBuilder();
				var dataModelHandlerFactory = GetDataModelHandlerFactory(smdBucket.GetString(Key.DataModelType), _dataModelHandlerFactories);
				_dataModelHandler = dataModelHandlerFactory.CreateHandler(interpreterModelBuilder);

				var entityMap = stateMachine != null ? interpreterModelBuilder.Build(stateMachine, _dataModelHandler, _customActionProviders).EntityMap : null;
				var restoredStateMachine = new StateMachineReader().Build(smdBucket, entityMap);

				if (stateMachine != null)
				{
					//TODO: Validate stateMachine vs restoredStateMachine (number of elements should be the same and documentId should point to the same entity type)
				}

				return interpreterModelBuilder.Build(restoredStateMachine, _dataModelHandler, _customActionProviders);
			}
		}

		private async ValueTask SaveInterpreterModel(InterpreterModel interpreterModel)
		{
			var storage = await _storageProvider.GetTransactionalStorage(partition: default, StateMachineDefinitionStorageKey, _stopToken).ConfigureAwait(false);
			await using (storage.ConfigureAwait(false))
			{
				SaveToStorage(interpreterModel.Root.As<IStoreSupport>(), new Bucket(storage));

				void SaveToStorage(IStoreSupport root, Bucket bucket)
				{
					var memoryStorage = new InMemoryStorage();
					root.Store(new Bucket(memoryStorage));

					Span<byte> span = stackalloc byte[memoryStorage.GetTransactionLogSize()];
					memoryStorage.WriteTransactionLogToSpan(span);

					bucket.Add(Key.Version, value: 1);
					bucket.Add(Key.SessionId, _sessionId);
					bucket.Add(Key.StateMachineDefinition, span);
				}

				await storage.CheckPoint(level: 0, _stopToken).ConfigureAwait(false);
			}
		}

		private static IDataModelHandlerFactory GetDataModelHandlerFactory(string dataModelType, ICollection<IDataModelHandlerFactory> factories)
		{
			if (factories != null)
			{
				foreach (var factory in factories)
				{
					if (factory.CanHandle(dataModelType ?? NoneDataModelHandler.DataModelType))
					{
						return factory;
					}
				}
			}

			switch (dataModelType)
			{
				case null:
				case NoneDataModelHandler.DataModelType:
					return NoneDataModelHandler.Factory;

				case RuntimeDataModelHandler.DataModelType:
					return RuntimeDataModelHandler.Factory;

				default:
					throw new InvalidOperationException($"Can't find DataModelHandlerFactory for DataModel type '{dataModelType}'");
			}
		}

		private ValueTask DoOperation(StateBagKey key, Func<ValueTask> func)
		{
			return IsPersistingEnabled ? DoOperationAsync() : func();

			async ValueTask DoOperationAsync()
			{
				var persistenceContext = _context.PersistenceContext;
				if (persistenceContext.GetState((int) key) == 0)
				{
					await func().ConfigureAwait(false);

					persistenceContext.SetState((int) key, value: 1);
				}
			}
		}

		private ValueTask DoOperation<TArg>(StateBagKey key, Func<TArg, ValueTask> func, TArg arg)
		{
			return IsPersistingEnabled ? DoOperationAsync() : func(arg);

			async ValueTask DoOperationAsync()
			{
				var persistenceContext = _context.PersistenceContext;
				if (persistenceContext.GetState((int) key) == 0)
				{
					await func(arg).ConfigureAwait(false);

					persistenceContext.SetState((int) key, value: 1);
				}
			}
		}

		private ValueTask DoOperation<TArg>(StateBagKey key, IEntity entity, Func<TArg, ValueTask> func, TArg arg)
		{
			return IsPersistingEnabled ? DoOperationAsync() : func(arg);

			async ValueTask DoOperationAsync()
			{
				var documentId = entity.As<IDocumentId>().DocumentId;

				var persistenceContext = _context.PersistenceContext;
				if (persistenceContext.GetState((int) key, documentId) == 0)
				{
					await func(arg).ConfigureAwait(false);

					persistenceContext.SetState((int) key, documentId, value: 1);
				}
			}
		}

		private void Complete(StateBagKey key)
		{
			if (IsPersistingEnabled)
			{
				_context.PersistenceContext.ClearState((int) key);
			}
		}

		private bool Capture(StateBagKey key, bool value)
		{
			if (IsPersistingEnabled)
			{
				var persistenceContext = _context.PersistenceContext;
				if (persistenceContext.GetState((int) key) == 1)
				{
					return persistenceContext.GetState((int) key, subKey: 0) == 1;
				}

				persistenceContext.SetState((int) key, subKey: 0, value ? 1 : 0);
				persistenceContext.SetState((int) key, value: 1);
			}

			return value;
		}

		private ValueTask<OrderedSet<TransitionNode>> Capture(StateBagKey key, Func<ValueTask<OrderedSet<TransitionNode>>> value)
		{
			return IsPersistingEnabled ? CaptureAsync() : value();

			async ValueTask<OrderedSet<TransitionNode>> CaptureAsync()
			{
				var persistenceContext = _context.PersistenceContext;
				if (persistenceContext.GetState((int) key) == 0)
				{
					var set = await value().ConfigureAwait(false);
					var list = set.ToList();
					persistenceContext.SetState((int) key, subKey: 0, list.Count);

					for (var i = 0; i < list.Count; i ++)
					{
						persistenceContext.SetState((int) key, i + 1, list[i].As<IDocumentId>().DocumentId);
					}

					persistenceContext.SetState((int) key, value: 1);

					return set;
				}

				var capturedSet = new OrderedSet<TransitionNode>();
				var length = persistenceContext.GetState((int) key, subKey: 0);
				for (var i = 0; i < length; i ++)
				{
					var documentId = persistenceContext.GetState((int) key, i + 1);
					capturedSet.Add(_model.EntityMap[documentId].As<TransitionNode>());
				}

				return capturedSet;
			}
		}

		private ValueTask NotifyAccepted() => _notifyStateChanged?.OnChanged(StateMachineInterpreterState.Accepted) ?? default;
		private ValueTask NotifyStarted()  => _notifyStateChanged?.OnChanged(StateMachineInterpreterState.Started) ?? default;
		private ValueTask NotifyExited()   => _notifyStateChanged?.OnChanged(StateMachineInterpreterState.Exited) ?? default;
		private ValueTask NotifyWaiting()  => _notifyStateChanged?.OnChanged(StateMachineInterpreterState.Waiting) ?? default;

		private async ValueTask<StateMachineResult> Run(IStateMachine stateMachine)
		{
			var exitStatus = StateMachineExitStatus.Completed;
			Exception exception = null;

			_model = await BuildInterpreterModel(stateMachine).ConfigureAwait(false);
			_context = await CreateContext().ConfigureAwait(false);
			await using (_context.ConfigureAwait(false))
			{
				try
				{
					await DoOperation(StateBagKey.NotifyAccepted, NotifyAccepted).ConfigureAwait(false);
					await DoOperation(StateBagKey.InitializeRootDataModel, InitializeRootDataModel).ConfigureAwait(false);
					await DoOperation(StateBagKey.EarlyInitializeDataModel, InitializeAllDataModels).ConfigureAwait(false);
					await DoOperation(StateBagKey.ExecuteGlobalScript, ExecuteGlobalScript).ConfigureAwait(false);
					await DoOperation(StateBagKey.NotifyStarted, NotifyStarted).ConfigureAwait(false);
					await DoOperation(StateBagKey.InitialEnterStates, InitialEnterStates).ConfigureAwait(false);
					await DoOperation(StateBagKey.MainEventLoop, MainEventLoop).ConfigureAwait(false);
					await DoOperation(StateBagKey.ExitInterpreter, ExitInterpreter).ConfigureAwait(false);
					await DoOperation(StateBagKey.NotifyExited, NotifyExited).ConfigureAwait(false);
				}
				catch (Exception ex) when (ConvertToStatus(ex, out var status))
				{
					exitStatus = status;
					exception = ex;
				}

				if (exitStatus == StateMachineExitStatus.Destroyed)
				{
					await DoOperation(StateBagKey.ExitInterpreter, ExitInterpreter).ConfigureAwait(false);
					await DoOperation(StateBagKey.NotifyExited, NotifyExited).ConfigureAwait(false);
				}

				if (IsPersistingEnabled)
				{
					switch (exitStatus)
					{
						case StateMachineExitStatus.Suspended:
							await PersistExternalEvents().ConfigureAwait(false);
							break;
						case StateMachineExitStatus.Completed:
						case StateMachineExitStatus.Destroyed:
						case StateMachineExitStatus.LiveLockAbort:
							await CleanupPersistedData().ConfigureAwait(false);
							break;
					}
				}
			}

			return exception == null ? new StateMachineResult(exitStatus, _doneData) : new StateMachineResult(exitStatus, exception);
		}

		private bool ConvertToStatus(Exception ex, out StateMachineExitStatus exitStatus)
		{
			exitStatus = ex switch
			{
					ChannelClosedException _ => StateMachineExitStatus.QueueClosed,
					StateMachineLiveLockException _ => StateMachineExitStatus.LiveLockAbort,
					OperationCanceledException e when e.CancellationToken == _stopToken => StateMachineExitStatus.Unknown,
					OperationCanceledException e when e.CancellationToken == _destroyToken => StateMachineExitStatus.Destroyed,
					OperationCanceledException e when e.CancellationToken == _suspendToken => StateMachineExitStatus.Suspended,
					OperationCanceledException e when e.CancellationToken == _anyToken && _stopToken.IsCancellationRequested => StateMachineExitStatus.Unknown,
					OperationCanceledException e when e.CancellationToken == _anyToken && _destroyToken.IsCancellationRequested => StateMachineExitStatus.Destroyed,
					OperationCanceledException e when e.CancellationToken == _anyToken && _suspendToken.IsCancellationRequested => StateMachineExitStatus.Suspended,
					_ => StateMachineExitStatus.Unknown
			};

			return exitStatus != StateMachineExitStatus.Unknown;
		}

		private ValueTask PersistExternalEvents()
		{
			var externalBufferedQueue = _context.ExternalBufferedQueue;

			while (_eventChannel.TryRead(out var @event))
			{
				externalBufferedQueue.Enqueue(@event);
			}

			return CheckPoint(PersistenceLevel.Event);
		}

		private async ValueTask CleanupPersistedData()
		{
			await _storageProvider.RemoveTransactionalStorage(partition: default, StateStorageKey, _stopToken).ConfigureAwait(false);
			await _storageProvider.RemoveTransactionalStorage(partition: default, StateMachineDefinitionStorageKey, _stopToken).ConfigureAwait(false);
		}

		private async ValueTask InitializeAllDataModels()
		{
			if (_model.Root.Binding == BindingType.Early)
			{
				foreach (var node in _model.DataModelList)
				{
					await DoOperation(StateBagKey.InitializeDataModel, node, InitializeDataModel, node).ConfigureAwait(false);
				}
			}
		}

		private ValueTask InitialEnterStates() => EnterStates(new[] { _model.Root.Initial.Transition });

		private async ValueTask MainEventLoop()
		{
			var exit = false;

			while (Capture(StateBagKey.Running, Running && !exit))
			{
				_anyToken.ThrowIfCancellationRequested();

				await DoOperation(StateBagKey.InternalQueueProcessing, InternalQueueProcessing).ConfigureAwait(false);

				exit = await ExternalQueueProcessing().ConfigureAwait(false);

				Complete(StateBagKey.InternalQueueProcessing);
				Complete(StateBagKey.Running);
			}

			Complete(StateBagKey.Running);
		}

		private async ValueTask InternalQueueProcessing()
		{
			var liveLockDetector = LiveLockDetector.Create();
			var exit = false;

			try
			{
				while (Capture(StateBagKey.Running, Running && !exit))
				{
					_anyToken.ThrowIfCancellationRequested();

					liveLockDetector.Iteration(_context.InternalQueue.Count);

					exit = await InternalQueueProcessingIteration().ConfigureAwait(false);

					Complete(StateBagKey.Running);
				}

				Complete(StateBagKey.Running);
			}
			finally
			{
				liveLockDetector.Dispose();
			}
		}

		private ValueTask<OrderedSet<TransitionNode>> SelectInternalEventTransitions()
		{
			var internalEvent = _context.InternalQueue.Dequeue();

			_logger.ProcessingEvent(internalEvent);

			_context.DataModel.SetInternal(property: "_event", new DataModelDescriptor(DataModelValue.FromEvent(internalEvent), isReadOnly: true));

			return SelectTransitions(internalEvent);
		}

		private async ValueTask<bool> InternalQueueProcessMessage()
		{
			var exit = false;

			if (Capture(StateBagKey.InternalQueueNonEmpty, _context.InternalQueue.Count > 0))
			{
				var transitions = await Capture(StateBagKey.SelectInternalEventTransitions, SelectInternalEventTransitions).ConfigureAwait(false);

				if (!transitions.IsEmpty)
				{
					await Microstep(transitions.ToList()).ConfigureAwait(false);

					await CheckPoint(PersistenceLevel.Transition).ConfigureAwait(false);
				}

				Complete(StateBagKey.SelectInternalEventTransitions);
			}
			else
			{
				exit = true;
			}

			Complete(StateBagKey.InternalQueueNonEmpty);

			return exit;
		}

		private async ValueTask<bool> InternalQueueProcessingIteration()
		{
			var exit = false;
			var transitions = await Capture(StateBagKey.EventlessTransitions, SelectEventlessTransitions).ConfigureAwait(false);

			if (!transitions.IsEmpty)
			{
				await Microstep(transitions.ToList()).ConfigureAwait(false);

				await CheckPoint(PersistenceLevel.Transition).ConfigureAwait(false);
			}
			else
			{
				exit = await InternalQueueProcessMessage().ConfigureAwait(false);
			}

			Complete(StateBagKey.EventlessTransitions);

			return exit;
		}

		private async ValueTask<bool> ExternalQueueProcessing()
		{
			var exit = false;

			if (Capture(StateBagKey.Running2, Running))
			{
				foreach (var state in _context.StatesToInvoke.ToSortedList(StateEntityNode.EntryOrder))
				{
					foreach (var invoke in state.Invoke)
					{
						await DoOperation(StateBagKey.Invoke, invoke, Invoke, invoke).ConfigureAwait(false);
					}
				}

				_context.StatesToInvoke.Clear();
				Complete(StateBagKey.Invoke);

				if (Capture(StateBagKey.InternalQueueEmpty, _context.InternalQueue.Count == 0))
				{
					_anyToken.ThrowIfCancellationRequested();

					var externalEvent = await ReadExternalEvent().ConfigureAwait(false);

					_logger.ProcessingEvent(externalEvent);

					_context.DataModel.SetInternal(property: "_event", new DataModelDescriptor(DataModelValue.FromEvent(externalEvent), isReadOnly: true));

					foreach (var state in _context.Configuration.ToList())
					{
						foreach (var invoke in state.Invoke)
						{
							if (invoke.InvokeUniqueId == externalEvent.InvokeUniqueId)
							{
								await ApplyFinalize(invoke).ConfigureAwait(false);
							}

							if (invoke.AutoForward)
							{
								await ForwardEvent(invoke.InvokeId, externalEvent).ConfigureAwait(false);
							}
						}
					}

					var transitions = await SelectTransitions(externalEvent).ConfigureAwait(false);

					if (!transitions.IsEmpty)
					{
						await Microstep(transitions.ToList()).ConfigureAwait(false);

						await CheckPoint(PersistenceLevel.Event).ConfigureAwait(false);
					}
				}

				Complete(StateBagKey.InternalQueueEmpty);
			}
			else
			{
				exit = true;
			}

			Complete(StateBagKey.Running2);

			return exit;
		}

		private async ValueTask CheckPoint(PersistenceLevel level)
		{
			if (!IsPersistingEnabled || _persistenceLevel < level)
			{
				return;
			}

			var persistenceContext = _context.PersistenceContext;
			await persistenceContext.CheckPoint((int) level, _stopToken).ConfigureAwait(false);

			if (level == PersistenceLevel.StableState)
			{
				await persistenceContext.Shrink(_stopToken).ConfigureAwait(false);
			}
		}

		private async ValueTask<IEvent> ReadExternalEvent()
		{
			while (true)
			{
				var @event = await ReadExternalEventUnfiltered().ConfigureAwait(false);

				if (@event.InvokeId == null)
				{
					return @event;
				}

				if (_externalCommunication.IsInvokeActive(@event.InvokeId, @event.InvokeUniqueId))
				{
					return @event;
				}
			}
		}

		private async ValueTask<IEvent> ReadExternalEventUnfiltered()
		{
			var externalBufferedQueue = _context.ExternalBufferedQueue;

			while (_eventChannel.TryRead(out var @event))
			{
				externalBufferedQueue.Enqueue(@event);
			}

			if (externalBufferedQueue.Count > 0)
			{
				return externalBufferedQueue.Dequeue();
			}

			await CheckPoint(PersistenceLevel.StableState).ConfigureAwait(false);

			await NotifyWaiting().ConfigureAwait(false);

			return await _eventChannel.ReadAsync(_anyToken).ConfigureAwait(false);
		}

		private async ValueTask ExitInterpreter()
		{
			var statesToExit = _context.Configuration.ToSortedList(StateEntityNode.ExitOrder);

			foreach (var state in statesToExit)
			{
				foreach (var onExit in state.OnExit)
				{
					await DoOperation(StateBagKey.OnExit, onExit, RunExecutableEntity, onExit.ActionEvaluators).ConfigureAwait(false);
				}

				foreach (var invoke in state.Invoke)
				{
					CancelInvoke(invoke);
				}

				_context.Configuration.Delete(state);

				if (state is FinalNode final && final.Parent is StateMachineNode)
				{
					await DoOperation(StateBagKey.ReturnDoneEvent, state, EvaluateDoneData, final).ConfigureAwait(false);
				}
			}

			Complete(StateBagKey.ReturnDoneEvent);
			Complete(StateBagKey.OnExit);
		}

		private ValueTask<OrderedSet<TransitionNode>> SelectEventlessTransitions() => SelectTransitions(@event: null);

		private async ValueTask<OrderedSet<TransitionNode>> SelectTransitions(IEvent @event)
		{
			var transitions = new OrderedSet<TransitionNode>();

			foreach (var state in _context.Configuration.ToFilteredSortedList(s => s.IsAtomicState, StateEntityNode.EntryOrder))
			{
				await FindTransitionForState(state).ConfigureAwait(false);
			}

			return RemoveConflictingTransitions(transitions);

			async ValueTask FindTransitionForState(StateEntityNode state)
			{
				foreach (var transition in state.Transitions)
				{
					if (EventMatch(transition) && await ConditionMatch(transition).ConfigureAwait(false))
					{
						transitions.Add(transition);

						return;
					}
				}

				if (!(state.Parent is StateMachineNode))
				{
					await FindTransitionForState(state.Parent).ConfigureAwait(false);
				}
			}

			bool EventMatch(TransitionNode transition)
			{
				var eventDescriptors = transition.Event;

				if (@event == null)
				{
					return eventDescriptors == null;
				}

				return eventDescriptors != null && eventDescriptors.Any(EventNameMatch);
			}

			bool EventNameMatch(IEventDescriptor eventDescriptor) => eventDescriptor.IsEventMatch(@event);

			async ValueTask<bool> ConditionMatch(TransitionNode transition)
			{
				var condition = transition.ConditionEvaluator;

				if (condition == null)
				{
					return true;
				}

				_stopToken.ThrowIfCancellationRequested();

				try
				{
					return await condition.EvaluateBoolean(_context.ExecutionContext, _stopToken).ConfigureAwait(false);
				}
				catch (Exception ex) when (IsError(ex))
				{
					await Error(transition, ex).ConfigureAwait(false);

					return false;
				}
			}
		}

		private OrderedSet<TransitionNode> RemoveConflictingTransitions(OrderedSet<TransitionNode> enabledTransitions)
		{
			var filteredTransitions = new OrderedSet<TransitionNode>();

			foreach (var t1 in enabledTransitions.ToList())
			{
				var t1Preempted = false;
				var transitionsToRemove = new OrderedSet<TransitionNode>();

				foreach (var t2 in filteredTransitions.ToList())
				{
					if (ComputeExitSet(new[] { t1 }).HasIntersection(ComputeExitSet(new[] { t2 })))
					{
						if (IsDescendant(t1.Source, t2.Source))
						{
							transitionsToRemove.Add(t2);
						}
						else
						{
							t1Preempted = true;
							break;
						}
					}
				}

				if (!t1Preempted)
				{
					foreach (var t3 in transitionsToRemove.ToList())
					{
						filteredTransitions.Delete(t3);
					}

					filteredTransitions.Add(t1);
				}
			}

			return filteredTransitions;
		}

		private async ValueTask Microstep(IReadOnlyList<TransitionNode> enabledTransitions)
		{
			await DoOperation(StateBagKey.ExitStates, ExitStates, enabledTransitions).ConfigureAwait(false);

			await DoOperation(StateBagKey.ExecuteTransitionContent, ExecuteTransitionContent, enabledTransitions).ConfigureAwait(false);

			await DoOperation(StateBagKey.EnterStates, EnterStates, enabledTransitions).ConfigureAwait(false);
		}

		private async ValueTask ExitStates(IReadOnlyList<TransitionNode> enabledTransitions)
		{
			var statesToExit = ComputeExitSet(enabledTransitions);

			foreach (var state in statesToExit.ToList())
			{
				_context.StatesToInvoke.Delete(state);
			}

			var states = statesToExit.ToSortedList(StateEntityNode.ExitOrder);

			foreach (var state in states)
			{
				foreach (var history in state.HistoryStates)
				{
					var predicate = history.Type == HistoryType.Deep ? (Predicate<StateEntityNode>) Deep : Shallow;

					bool Deep(StateEntityNode node)    => node.IsAtomicState && IsDescendant(node, state);
					bool Shallow(StateEntityNode node) => node.Parent == state;

					_context.HistoryValue.Set(history.Id, _context.Configuration.ToFilteredList(predicate));
				}
			}

			foreach (var state in states)
			{
				_logger.ExitingState(state);

				foreach (var onExit in state.OnExit)
				{
					await DoOperation(StateBagKey.OnExit, onExit, RunExecutableEntity, onExit.ActionEvaluators).ConfigureAwait(false);
				}

				foreach (var invoke in state.Invoke)
				{
					CancelInvoke(invoke);
				}

				_context.Configuration.Delete(state);
			}

			Complete(StateBagKey.OnExit);
		}

		private async ValueTask EnterStates(IReadOnlyList<TransitionNode> enabledTransitions)
		{
			var statesToEnter = new OrderedSet<StateEntityNode>();
			var statesForDefaultEntry = new OrderedSet<CompoundNode>();
			var defaultHistoryContent = new DefaultHistoryContent();

			ComputeEntrySet(enabledTransitions, statesToEnter, statesForDefaultEntry, defaultHistoryContent);

			foreach (var state in statesToEnter.ToSortedList(StateEntityNode.EntryOrder))
			{
				_context.Configuration.Add(state);
				_context.StatesToInvoke.Add(state);

				if (_model.Root.Binding == BindingType.Late)
				{
					await DoOperation(StateBagKey.InitializeDataModel, state.DataModel, InitializeDataModel, state.DataModel).ConfigureAwait(false);
				}

				_logger.EnteringState(state);

				foreach (var onEntry in state.OnEntry)
				{
					await DoOperation(StateBagKey.OnEntry, onEntry, RunExecutableEntity, onEntry.ActionEvaluators).ConfigureAwait(false);
				}

				if (state is CompoundNode compound && statesForDefaultEntry.IsMember(compound))
				{
					await DoOperation(StateBagKey.DefaultEntry, state, RunExecutableEntity, compound.Initial.Transition.ActionEvaluators).ConfigureAwait(false);
				}

				if (defaultHistoryContent.TryGetValue(state.Id, out var action))
				{
					await DoOperation(StateBagKey.DefaultHistoryContent, state, RunExecutableEntity, action).ConfigureAwait(false);
				}

				if (state is FinalNode final)
				{
					if (final.Parent is StateMachineNode)
					{
						Running = false;
					}
					else
					{
						var parent = final.Parent;
						var grandparent = parent.Parent;

						DataModelValue doneData = default;
						if (final.DoneData != null)
						{
							doneData = await EvaluateDoneData(final.DoneData).ConfigureAwait(false);
						}

						_context.InternalQueue.Enqueue(new EventObject(EventType.Internal, EventName.GetDoneStateNameParts(parent.Id), doneData));

						if (grandparent is ParallelNode)
						{
							if (grandparent.States.All(IsInFinalState))
							{
								_context.InternalQueue.Enqueue(new EventObject(EventType.Internal, EventName.GetDoneStateNameParts(grandparent.Id)));
							}
						}
					}
				}
			}

			Complete(StateBagKey.OnEntry);
			Complete(StateBagKey.DefaultEntry);
			Complete(StateBagKey.DefaultHistoryContent);
		}

		private async ValueTask<DataModelValue> EvaluateDoneData(DoneDataNode doneData)
		{
			try
			{
				return await doneData.Evaluate(_context.ExecutionContext, _stopToken).ConfigureAwait(false);
			}
			catch (Exception ex) when (IsError(ex))
			{
				await Error(doneData, ex).ConfigureAwait(false);
			}

			return default;
		}

		private bool IsInFinalState(StateEntityNode state)
		{
			if (state is CompoundNode)
			{
				return state.States.Any(s => s is FinalNode && _context.Configuration.IsMember(s));
			}

			if (state is ParallelNode)
			{
				return state.States.All(IsInFinalState);
			}

			return false;
		}

		private void ComputeEntrySet(IReadOnlyList<TransitionNode> transitions, OrderedSet<StateEntityNode> statesToEnter, OrderedSet<CompoundNode> statesForDefaultEntry,
									 DefaultHistoryContent defaultHistoryContent)
		{
			foreach (var transition in transitions)
			{
				foreach (var state in transition.TargetState)
				{
					AddDescendantStatesToEnter(state, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
				}

				var ancestor = GetTransitionDomain(transition);

				foreach (var state in GetEffectiveTargetStates(transition).ToList())
				{
					AddAncestorStatesToEnter(state, ancestor, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
				}
			}
		}

		private OrderedSet<StateEntityNode> ComputeExitSet(IReadOnlyList<TransitionNode> transitions)
		{
			var statesToExit = new OrderedSet<StateEntityNode>();
			foreach (var transition in transitions)
			{
				if (transition.Target != null)
				{
					var domain = GetTransitionDomain(transition);
					foreach (var state in _context.Configuration.ToList())
					{
						if (IsDescendant(state, domain))
						{
							statesToExit.Add(state);
						}
					}
				}
			}

			return statesToExit;
		}

		private void AddDescendantStatesToEnter(StateEntityNode state, OrderedSet<StateEntityNode> statesToEnter, OrderedSet<CompoundNode> statesForDefaultEntry,
												DefaultHistoryContent defaultHistoryContent)
		{
			if (state is HistoryNode history)
			{
				if (_context.HistoryValue.TryGetValue(history.Id, out var states))
				{
					foreach (var s in states)
					{
						AddDescendantStatesToEnter(s, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
					}

					foreach (var s in states)
					{
						AddAncestorStatesToEnter(s, state.Parent, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
					}
				}
				else
				{
					defaultHistoryContent[state.Parent.Id] = history.Transition.ActionEvaluators;

					foreach (var s in history.Transition.TargetState)
					{
						AddDescendantStatesToEnter(s, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
					}

					foreach (var s in history.Transition.TargetState)
					{
						AddAncestorStatesToEnter(s, state.Parent, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
					}
				}
			}
			else
			{
				statesToEnter.Add(state);
				if (state is CompoundNode compound)
				{
					statesForDefaultEntry.Add(compound);

					foreach (var s in compound.Initial.Transition.TargetState)
					{
						AddDescendantStatesToEnter(s, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
					}

					foreach (var s in compound.Initial.Transition.TargetState)
					{
						AddAncestorStatesToEnter(s, state, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
					}
				}
				else
				{
					if (state is ParallelNode)
					{
						foreach (var child in state.States)
						{
							if (!statesToEnter.Some(s => IsDescendant(s, child)))
							{
								AddDescendantStatesToEnter(child, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
							}
						}
					}
				}
			}
		}

		private void AddAncestorStatesToEnter(StateEntityNode state, StateEntityNode ancestor, OrderedSet<StateEntityNode> statesToEnter, OrderedSet<CompoundNode> statesForDefaultEntry,
											  DefaultHistoryContent defaultHistoryContent)
		{
			foreach (var anc in GetProperAncestors(state, ancestor).ToList())
			{
				statesToEnter.Add(anc);

				if (anc is ParallelNode)
				{
					foreach (var child in anc.States)
					{
						if (!statesToEnter.Some(s => IsDescendant(s, child)))
						{
							AddDescendantStatesToEnter(child, statesToEnter, statesForDefaultEntry, defaultHistoryContent);
						}
					}
				}
			}
		}

		private static bool IsDescendant(StateEntityNode state1, StateEntityNode state2)
		{
			for (var s = state1.Parent; s != null; s = s.Parent)
			{
				if (s == state2)
				{
					return true;
				}
			}

			return false;
		}

		private StateEntityNode GetTransitionDomain(TransitionNode transition)
		{
			var tstates = GetEffectiveTargetStates(transition);

			if (tstates.IsEmpty)
			{
				return null;
			}

			if (transition.Type == TransitionType.Internal && transition.Source is CompoundNode && tstates.Every(s => IsDescendant(s, transition.Source)))
			{
				return transition.Source;
			}

			return FindLcca(transition.Source, tstates);
		}

		private StateEntityNode FindLcca(StateEntityNode headState, OrderedSet<StateEntityNode> tailStates)
		{
			foreach (var anc in GetProperAncestors(headState, state2: null).ToList())
			{
				if (tailStates.Every(s => IsDescendant(s, anc)))
				{
					return anc;
				}
			}

			return null;
		}

		private OrderedSet<StateEntityNode> GetProperAncestors(StateEntityNode state1, StateEntityNode state2)
		{
			var states = new OrderedSet<StateEntityNode>();

			for (var s = state1.Parent; s != null; s = s.Parent)
			{
				if (s == state2)
				{
					return states;
				}

				states.Add(s);
			}

			return state2 == null ? states : new OrderedSet<StateEntityNode>();
		}

		private OrderedSet<StateEntityNode> GetEffectiveTargetStates(TransitionNode transition)
		{
			var targets = new OrderedSet<StateEntityNode>();

			foreach (var state in transition.TargetState)
			{
				if (state is HistoryNode history)
				{
					if (!_context.HistoryValue.TryGetValue(history.Id, out var values))
					{
						values = GetEffectiveTargetStates(history.Transition).ToList();
					}

					targets.Union(values);
				}
				else
				{
					targets.Add(state);
				}
			}

			return targets;
		}

		private async ValueTask ExecuteTransitionContent(IReadOnlyList<TransitionNode> transitions)
		{
			foreach (var transition in transitions)
			{
				_logger.PerformingTransition(transition);

				await DoOperation(StateBagKey.RunExecutableEntity, transition, RunExecutableEntity, transition.ActionEvaluators).ConfigureAwait(false);
			}

			Complete(StateBagKey.RunExecutableEntity);
		}

		private async ValueTask RunExecutableEntity(IReadOnlyList<IExecEvaluator> action)
		{
			foreach (var executableEntity in action)
			{
				_stopToken.ThrowIfCancellationRequested();

				try
				{
					await executableEntity.Execute(_context.ExecutionContext, _stopToken).ConfigureAwait(false);
				}
				catch (Exception ex) when (IsError(ex))
				{
					await Error(executableEntity, ex).ConfigureAwait(false);

					break;
				}
			}

			await CheckPoint(PersistenceLevel.ExecutableAction).ConfigureAwait(false);
		}

		private bool IsOperationCancelled(Exception ex)
		{
			return ex switch
			{
					OperationCanceledException operationCanceledException => (operationCanceledException.CancellationToken == _stopToken),
					_ => false
			};
		}

		private bool IsError(Exception ex) => !IsOperationCancelled(ex);

		private async ValueTask Error(object source, Exception exception, bool logLoggerErrors = true)
		{
			var sourceEntityId = (source as IEntity).Is(out IDebugEntityId id) ? id.EntityId.ToString(CultureInfo.InvariantCulture) : null;

			string sendId = null;

			var errorType = _logger.IsPlatformError(exception)
					? ErrorType.Platform
					: _externalCommunication.IsCommunicationError(exception, out sendId)
							? ErrorType.Communication
							: ErrorType.Execution;

			var nameParts = errorType switch
			{
					ErrorType.Execution => EventName.ErrorExecution,
					ErrorType.Communication => EventName.ErrorCommunication,
					ErrorType.Platform => EventName.ErrorPlatform,
					_ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, message: null)
			};

			var eventObject = new EventObject(EventType.Platform, nameParts, DataModelValue.FromException(exception), sendId);

			_context.InternalQueue.Enqueue(eventObject);

			try
			{
				await _logger.Error(errorType, _model.Root.Name, sourceEntityId, exception, _stopToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (logLoggerErrors)
				{
					try
					{
						await Error(source, ex, logLoggerErrors: false).ConfigureAwait(false);
					}
					catch
					{
						// ignored
					}
				}
			}
		}

		private async ValueTask ExecuteGlobalScript()
		{
			if (_model.Root.ScriptEvaluator != null)
			{
				try
				{
					await _model.Root.ScriptEvaluator.Execute(_context.ExecutionContext, _stopToken).ConfigureAwait(false);
				}
				catch (Exception ex) when (IsError(ex))
				{
					await Error(_model.Root.ScriptEvaluator, ex).ConfigureAwait(false);
				}
			}
		}

		private async ValueTask EvaluateDoneData(FinalNode final)
		{
			if (final.DoneData != null)
			{
				_doneData = await EvaluateDoneData(final.DoneData).ConfigureAwait(false);
			}
		}

		private ValueTask ForwardEvent(string invokeId, IEvent @event) => _externalCommunication.ForwardEvent(@event, invokeId, _stopToken);

		private ValueTask ApplyFinalize(InvokeNode invoke) => invoke.Finalize != null ? RunExecutableEntity(invoke.Finalize.ActionEvaluators) : default;

		private async ValueTask Invoke(InvokeNode invoke)
		{
			try
			{
				await invoke.Start(_context.ExecutionContext, _stopToken).ConfigureAwait(false);
			}
			catch (Exception ex) when (IsError(ex))
			{
				await Error(invoke, ex).ConfigureAwait(false);
			}
		}

		private async void CancelInvoke(InvokeNode invoke)
		{
			try
			{
				await invoke.Cancel(_context.ExecutionContext, _stopToken).ConfigureAwait(false);
			}
			catch (Exception ex) when (IsError(ex))
			{
				await Error(invoke, ex).ConfigureAwait(false);
			}
		}

		private async ValueTask InitializeRootDataModel()
		{
			var rootDataModel = _model.Root.DataModel;

			if (rootDataModel == null)
			{
				return;
			}

			if (_arguments.Type != DataModelValueType.Object)
			{
				await InitializeDataModel(rootDataModel).ConfigureAwait(false);

				return;
			}

			var dictionary = _arguments.AsObject();
			foreach (var node in rootDataModel.Data)
			{
				await InitializeData(node, dictionary[node.Id]).ConfigureAwait(false);
			}
		}

		private async ValueTask InitializeDataModel(DataModelNode dataModel)
		{
			foreach (var node in dataModel.Data)
			{
				await InitializeData(node).ConfigureAwait(false);
			}
		}

		private async ValueTask InitializeData(DataNode data, DataModelValue overrideValue = default)
		{
			try
			{
				_context.DataModel[data.Id] = await GetValue().ConfigureAwait(false);
			}
			catch (Exception ex) when (IsError(ex))
			{
				await Error(data, ex).ConfigureAwait(false);
			}

			async ValueTask<DataModelValue> GetValue()
			{
				if (overrideValue.Type != DataModelValueType.Undefined)
				{
					return overrideValue;
				}

				if (data.Source != null)
				{
					var resource = await _resourceLoader.Request(data.Source.Uri, _stopToken).ConfigureAwait(false);

					return DataModelValue.FromContent(resource.Content, resource.ContentType);
				}

				if (data.ExpressionEvaluator != null)
				{
					var obj = await data.ExpressionEvaluator.EvaluateObject(_context.ExecutionContext, _stopToken).ConfigureAwait(false);

					return DataModelValue.FromObject(obj.ToObject());
				}

				if (data.InlineContent != null)
				{
					return DataModelValue.FromInlineContent(data.InlineContent);
				}

				return DataModelValue.Undefined;
			}
		}

		private async ValueTask<IStateMachineContext> CreateContext()
		{
			IStateMachineContext context;
			if (IsPersistingEnabled)
			{
				var storage = await _storageProvider.GetTransactionalStorage(partition: default, StateStorageKey, _stopToken).ConfigureAwait(false);
				context = new StateMachinePersistedContext(_model.Root.Name, _sessionId, _arguments, storage, _model.EntityMap, _logger, _externalCommunication);
			}
			else
			{
				context = new StateMachineContext(_model.Root.Name, _sessionId, _arguments, _logger, _externalCommunication);
			}

			PopulateInterpreterObject(context.InterpreterObject);

			var dataModelVars = new Dictionary<string, string>();
			_dataModelHandler.ExecutionContextCreated(context.ExecutionContext, dataModelVars);

			PopulateDataModelHandlerObject(context.DataModelHandlerObject, dataModelVars);

			return context;
		}

		private void PopulateInterpreterObject(DataModelObject interpreterObject)
		{
			var type = GetType();
			var version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			interpreterObject.SetInternal(property: "name", new DataModelDescriptor(new DataModelValue(type.FullName)));
			interpreterObject.SetInternal(property: "version", new DataModelDescriptor(new DataModelValue(version)));
		}

		private void PopulateDataModelHandlerObject(DataModelObject dataModelHandlerObject, Dictionary<string, string> dataModelVars)
		{
			var type = _dataModelHandler.GetType();
			var version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			dataModelHandlerObject.SetInternal(property: "name", new DataModelDescriptor(new DataModelValue(type.FullName)));
			dataModelHandlerObject.SetInternal(property: "assembly", new DataModelDescriptor(new DataModelValue(type.Assembly.GetName().Name)));
			dataModelHandlerObject.SetInternal(property: "version", new DataModelDescriptor(new DataModelValue(version)));

			var vars = new DataModelObject(isReadOnly: true);
			foreach (var pair in dataModelVars)
			{
				vars.SetInternal(pair.Key, new DataModelDescriptor(new DataModelValue(pair.Value)));
			}

			dataModelHandlerObject.SetInternal(property: "vars", new DataModelDescriptor(new DataModelValue(vars)));
		}

		private enum StateBagKey
		{
			Stop,
			Running,
			Running2,
			EventlessTransitions,
			InternalQueueNonEmpty,
			SelectInternalEventTransitions,
			InternalQueueEmpty,
			InitializeRootDataModel,
			EarlyInitializeDataModel,
			ExecuteGlobalScript,
			InitialEnterStates,
			ExitInterpreter,
			InternalQueueProcessing,
			MainEventLoop,
			ExitStates,
			ExecuteTransitionContent,
			EnterStates,
			RunExecutableEntity,
			InitializeDataModel,
			OnExit,
			OnEntry,
			DefaultEntry,
			DefaultHistoryContent,
			Invoke,
			ReturnDoneEvent,
			NotifyAccepted,
			NotifyStarted,
			NotifyExited
		}

		private struct LiveLockDetector : IDisposable
		{
			private const int IterationCount = 36;

			private int   _internalQueueLength;
			private int   _index;
			private int   _sum;
			private int[] _data;

			public static LiveLockDetector Create() => new LiveLockDetector { _index = -1 };

			public void Iteration(int internalQueueCount)
			{
				if (_index == -1)
				{
					_internalQueueLength = internalQueueCount;
					_index = _sum = 0;

					return;
				}

				if (_data == null)
				{
					_data = ArrayPool<int>.Shared.Rent(IterationCount);
				}

				if (_index >= IterationCount)
				{
					if (_sum >= 0)
					{
						throw new StateMachineLiveLockException();
					}

					_sum -= _data[_index % IterationCount];
				}

				var delta = internalQueueCount - _internalQueueLength;
				_internalQueueLength = internalQueueCount;
				_sum += delta;
				_data[_index ++ % IterationCount] = delta;
			}

			public void Dispose()
			{
				if (_data != null)
				{
					ArrayPool<int>.Shared.Return(_data);
				}
			}
		}
	}
}