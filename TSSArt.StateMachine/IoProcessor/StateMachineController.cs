using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class StateMachineController : IService, IExternalCommunication, INotifyStateChanged, IAsyncDisposable
	{
		private static readonly UnboundedChannelOptions UnboundedSynchronousChannelOptions  = new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = true };
		private static readonly UnboundedChannelOptions UnboundedAsynchronousChannelOptions = new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false };

		private readonly TaskCompletionSource<int>            _acceptedTcs  = new TaskCompletionSource<int>();
		private readonly TaskCompletionSource<DataModelValue> _completedTcs = new TaskCompletionSource<DataModelValue>();
		private readonly InterpreterOptions                   _defaultOptions;
		private readonly CancellationTokenSource              _destroyTokenSource;
		private readonly TimeSpan                             _idlePeriod;
		private readonly IIoProcessor                         _ioProcessor;
		private readonly ILogger                              _logger;
		private readonly HashSet<ScheduledEvent>              _scheduledEvents = new HashSet<ScheduledEvent>();
		private readonly IStateMachine                        _stateMachine;
		private readonly ConcurrentQueue<ScheduledEvent>      _toDelete = new ConcurrentQueue<ScheduledEvent>();

		private bool                     _disposed;
		private CancellationTokenSource? _suspendOnIdleTokenSource;
		private CancellationTokenSource? _suspendTokenSource;

		public StateMachineController(string sessionId, IStateMachineOptions? options, IStateMachine stateMachine, IIoProcessor ioProcessor, TimeSpan idlePeriod, in InterpreterOptions defaultOptions)
		{
			SessionId = sessionId;
			_stateMachine = stateMachine;
			_ioProcessor = ioProcessor;
			_defaultOptions = defaultOptions;
			_idlePeriod = idlePeriod;
			_logger = _defaultOptions.Logger ?? DefaultLogger.Instance;

			_destroyTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_defaultOptions.DestroyToken, token2: default);
			_defaultOptions.DestroyToken = _destroyTokenSource.Token;

			Channel = CreateChannel(options);
		}

		protected virtual Channel<IEvent> Channel { get; }

		public string SessionId { get; }

	#region Interface IAsyncDisposable

		public virtual ValueTask DisposeAsync()
		{
			if (_disposed)
			{
				return default;
			}

			_destroyTokenSource.Dispose();
			_suspendTokenSource?.Dispose();
			_suspendOnIdleTokenSource?.Dispose();

			_disposed = true;

			return default;
		}

	#endregion

	#region Interface IExternalCommunication

		ImmutableArray<IEventProcessor> IExternalCommunication.GetIoProcessors() => _ioProcessor.GetIoProcessors();

		async ValueTask<SendStatus> IExternalCommunication.TrySendEvent(IOutgoingEvent evt, CancellationToken token)
		{
			var sendStatus = await _ioProcessor.DispatchEvent(SessionId, evt, skipDelay: false, token).ConfigureAwait(false);

			if (sendStatus == SendStatus.ToSchedule)
			{
				await ScheduleEvent(evt, token).ConfigureAwait(false);

				return SendStatus.Sent;
			}

			return sendStatus;
		}

		async ValueTask IExternalCommunication.CancelEvent(string sendId, CancellationToken token)
		{
			foreach (var evt in _scheduledEvents)
			{
				if (evt.Event.SendId == sendId)
				{
					await CancelEvent(evt, token).ConfigureAwait(false);
				}
			}

			CleanScheduledEvents();
		}

		ValueTask IExternalCommunication.StartInvoke(InvokeData invokeData, CancellationToken token) => _ioProcessor.StartInvoke(SessionId, invokeData, token);

		ValueTask IExternalCommunication.CancelInvoke(string invokeId, CancellationToken token) => _ioProcessor.CancelInvoke(SessionId, invokeId, token);

		bool IExternalCommunication.IsInvokeActive(string invokeId, string invokeUniqueId) => _ioProcessor.IsInvokeActive(SessionId, invokeId, invokeUniqueId);

		ValueTask IExternalCommunication.ForwardEvent(IEvent evt, string invokeId, CancellationToken token) => _ioProcessor.ForwardEvent(SessionId, evt, invokeId, token);

	#endregion

	#region Interface INotifyStateChanged

		ValueTask INotifyStateChanged.OnChanged(StateMachineInterpreterState state)
		{
			if (state == StateMachineInterpreterState.Accepted)
			{
				_acceptedTcs.TrySetResult(0);
			}
			else if (state == StateMachineInterpreterState.Waiting)
			{
				_suspendOnIdleTokenSource?.CancelAfter(_idlePeriod);
			}

			return default;
		}

	#endregion

	#region Interface IService

		public ValueTask Send(IEvent evt, CancellationToken token) => Channel.Writer.WriteAsync(evt, token);

		ValueTask IService.Destroy(CancellationToken token)
		{
			_destroyTokenSource.Cancel();
			Channel.Writer.Complete();
			return default;
		}

		public Task<DataModelValue> Result => _completedTcs.Task;

	#endregion

		private static Channel<IEvent> CreateChannel(IStateMachineOptions? options)
		{
			if (options == null)
			{
				return System.Threading.Channels.Channel.CreateUnbounded<IEvent>(UnboundedAsynchronousChannelOptions);
			}

			var sync = options.SynchronousEventProcessing ?? false;
			var queueSize = options.ExternalQueueSize ?? 0;

			if (options.IsStateMachinePersistable() || queueSize <= 0)
			{
				return System.Threading.Channels.Channel.CreateUnbounded<IEvent>(sync ? UnboundedSynchronousChannelOptions : UnboundedAsynchronousChannelOptions);
			}

			var channelOptions = new BoundedChannelOptions(queueSize) { AllowSynchronousContinuations = sync, SingleReader = true };

			return System.Threading.Channels.Channel.CreateBounded<IEvent>(channelOptions);
		}

		public ValueTask StartAsync(CancellationToken token)
		{
			token.Register(() => _acceptedTcs.TrySetCanceled(token));

			ExecuteAsync().Forget();

			return new ValueTask(_acceptedTcs.Task);
		}

		private void FillOptions(out InterpreterOptions options)
		{
			options = _defaultOptions;

			options.ExternalCommunication = this;
			options.StorageProvider = this as IStorageProvider;
			options.NotifyStateChanged = this;

			if (_idlePeriod > TimeSpan.Zero)
			{
				_suspendTokenSource?.Dispose();
				_suspendOnIdleTokenSource?.Dispose();

				_suspendOnIdleTokenSource = new CancellationTokenSource(_idlePeriod);

				_suspendTokenSource = options.SuspendToken.CanBeCanceled
						? CancellationTokenSource.CreateLinkedTokenSource(options.SuspendToken, _suspendOnIdleTokenSource.Token)
						: _suspendOnIdleTokenSource;

				options.SuspendToken = _suspendTokenSource.Token;
			}
		}

		protected virtual ValueTask Initialize() => default;

		public async ValueTask<DataModelValue> ExecuteAsync()
		{
			var initialized = false;
			while (true)
			{
				try
				{
					if (!initialized)
					{
						initialized = true;

						await Initialize().ConfigureAwait(false);
					}

					FillOptions(out var options);

					try
					{
						var result = await StateMachineInterpreter.RunAsync(SessionId, _stateMachine, Channel.Reader, options).ConfigureAwait(false);
						_acceptedTcs.TrySetResult(0);
						_completedTcs.TrySetResult(result);

						return result;
					}
					catch (StateMachineSuspendedException) when (!_defaultOptions.SuspendToken.IsCancellationRequested) { }

					await WaitForResume().ConfigureAwait(false);
				}
				catch (OperationCanceledException ex)
				{
					_acceptedTcs.TrySetCanceled(ex.CancellationToken);
					_completedTcs.TrySetCanceled(ex.CancellationToken);

					throw;
				}
				catch (Exception ex)
				{
					_acceptedTcs.TrySetException(ex);
					_completedTcs.TrySetException(ex);

					throw;
				}
			}
		}

		private async ValueTask WaitForResume()
		{
			var anyTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_defaultOptions.StopToken, _defaultOptions.DestroyToken, _defaultOptions.SuspendToken);
			try
			{
				if (await Channel.Reader.WaitToReadAsync(anyTokenSource.Token).ConfigureAwait(false))
				{
					return;
				}

				await Channel.Reader.ReadAsync(CancellationToken.None).ConfigureAwait(false);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == anyTokenSource.Token && _defaultOptions.StopToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(Resources.Exception_State_Machine_has_been_halted, ex, _defaultOptions.StopToken);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == anyTokenSource.Token && _defaultOptions.SuspendToken.IsCancellationRequested)
			{
				throw new StateMachineSuspendedException(Resources.Exception_State_Machine_has_been_suspended, ex, _defaultOptions.SuspendToken);
			}
			catch (ChannelClosedException ex)
			{
				throw new StateMachineQueueClosedException(Resources.Exception_State_Machine_external_queue_has_been_closed, ex);
			}
			finally
			{
				anyTokenSource.Dispose();
			}
		}

		protected virtual ValueTask ScheduleEvent(IOutgoingEvent evt, CancellationToken token)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			var scheduledEvent = new ScheduledEvent(evt);

			_scheduledEvents.Add(scheduledEvent);

			DelayedFire(scheduledEvent, evt.DelayMs).Forget();

			CleanScheduledEvents();

			return default;
		}

		private void CleanScheduledEvents()
		{
			while (_toDelete.TryDequeue(out var scheduledEvent))
			{
				_scheduledEvents.Remove(scheduledEvent);
			}
		}

		protected async ValueTask DelayedFire(ScheduledEvent scheduledEvent, int delayMs)
		{
			if (scheduledEvent == null) throw new ArgumentNullException(nameof(scheduledEvent));

			try
			{
				await Task.Delay(delayMs, scheduledEvent.CancellationToken).ConfigureAwait(false);

				await CancelEvent(scheduledEvent, token: default).ConfigureAwait(false);

				try
				{
					await _ioProcessor.DispatchEvent(SessionId, scheduledEvent.Event, skipDelay: true, CancellationToken.None).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					await _logger.LogError(ErrorType.Communication, SessionId, _stateMachine.Name, scheduledEvent.Event.SendId, ex, token: default).ConfigureAwait(false);
				}
			}
			finally
			{
				scheduledEvent.Dispose();
			}
		}

		protected virtual ValueTask CancelEvent(ScheduledEvent scheduledEvent, CancellationToken token)
		{
			if (scheduledEvent == null) throw new ArgumentNullException(nameof(scheduledEvent));

			scheduledEvent.Cancel();

			_toDelete.Enqueue(scheduledEvent);

			return default;
		}

		protected class ScheduledEvent
		{
			private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

			public ScheduledEvent(IOutgoingEvent evt) => Event = evt;

			public IOutgoingEvent Event { get; }

			public CancellationToken CancellationToken => _cancellationTokenSource.Token;

			public void Cancel() => _cancellationTokenSource.Cancel();

			public void Dispose() => _cancellationTokenSource.Dispose();
		}
	}
}