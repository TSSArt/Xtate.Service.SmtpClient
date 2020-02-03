using System;
using System.Collections./**/Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class InterpreterModelBuilder : StateMachineVisitor
	{
		private int _counter;

		private IReadOnlyCollection<ICustomActionProvider>        _customActionProviders;
		private IDataModelHandler                                 _dataModelHandler;
		private List<DataModelNode>                               _dataModelNodeList;
		private int                                               _deepLevel;
		private LinkedList<int>                                   _documentIdList;
		private List<IEntity>                                     _entities;
		private Dictionary<int, IEntity>                          _entityMap;
		private List<(Uri Uri, IExternalScriptConsumer Consumer)> _externalScriptList;
		private Dictionary<IIdentifier, StateEntityNode>          _idMap;
		private bool                                              _inParallel;
		private List<TransitionNode>                              _targetMap;

		public InterpreterModelBuilder() : base(trackPath: true) { }

		private void CounterBefore(bool inParallel, out (int counter, bool inParallel) saved)
		{
			saved.counter = _counter;
			saved.inParallel = _inParallel;

			_counter = 0;
			_inParallel = inParallel;
		}

		private void CounterAfter((int counter, bool inParallel) saved)
		{
			_inParallel = saved.inParallel;
			_counter ++;
			_counter = _inParallel ? _counter + saved.counter : _counter > saved.counter ? _counter : saved.counter;
		}

		public InterpreterModel Build(IStateMachine stateMachine, IDataModelHandler dataModelHandler, IReadOnlyCollection<ICustomActionProvider> customActionProviders)
		{
			if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));

			_dataModelHandler = dataModelHandler ?? throw new ArgumentNullException(nameof(dataModelHandler));
			_customActionProviders = customActionProviders;
			_idMap = new Dictionary<IIdentifier, StateEntityNode>(IdentifierEqualityComparer.Instance);
			_entities = new List<IEntity>();
			_targetMap = new List<TransitionNode>();
			_dataModelNodeList = new List<DataModelNode>();
			_documentIdList = new LinkedList<int>();
			_externalScriptList = null;
			_inParallel = false;
			_deepLevel = 0;
			_counter = 0;

			Visit(ref stateMachine);

			ThrowIfErrors();

			CreateEntityMap();

			foreach (var transition in _targetMap)
			{
				transition.MapTarget(_idMap);
			}

			return new InterpreterModel((StateMachineNode) stateMachine, _counter, _entityMap, _dataModelNodeList);
		}

		public async ValueTask<InterpreterModel> Build(IStateMachine stateMachine, IDataModelHandler dataModelHandler, IReadOnlyCollection<ICustomActionProvider> customActionProviders,
													   IResourceLoader resourceLoader, CancellationToken token)
		{
			if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));
			if (resourceLoader == null) throw new ArgumentNullException(nameof(resourceLoader));

			var model = Build(stateMachine, dataModelHandler, customActionProviders);

			if (_externalScriptList != null)
			{
				await SetExternalResources(resourceLoader, token).ConfigureAwait(false);
			}

			return model;
		}

		private void CreateEntityMap()
		{
			var id = 0;
			for (var node = _documentIdList.First; node != null; node = node.Next)
			{
				node.Value = id ++;
			}

			_entityMap = new Dictionary<int, IEntity>();
			foreach (var entity in _entities)
			{
				if (entity.Is<IDocumentId>(out var autoDocId))
				{
					_entityMap.Add(autoDocId.DocumentId, entity);
				}
			}
		}

		private async ValueTask SetExternalResources(IResourceLoader resourceLoader, CancellationToken token)
		{
			foreach (var (uri, consumer) in _externalScriptList)
			{
				var resource = await resourceLoader.Request(uri, token).ConfigureAwait(false);
				consumer.SetContent(resource.Content);
			}
		}

		private void RegisterEntity(IEntity entity) => _entities.Add(entity);

		private LinkedListNode<int> NewDocumentId() => _documentIdList.AddLast(-1);

		private LinkedListNode<int> NewDocumentIdAfter(LinkedListNode<int> node) => _documentIdList.AddAfter(node, value: -1);

		protected override void Build(ref IStateMachine stateMachine, ref StateMachine stateMachineProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref stateMachine, ref stateMachineProperties);

			if (stateMachineProperties.States?.Count > 0 && stateMachineProperties.Initial == null)
			{
				var initialDocId = NewDocumentIdAfter(documentId);
				var target = new[] { stateMachineProperties.States[index: 0].As<StateEntityNode>() };
				var transition = new TransitionNode(NewDocumentIdAfter(initialDocId), transition: default, target);
				stateMachineProperties.Initial = new InitialNode(initialDocId, transition);
			}

			var stateMachineNode = new StateMachineNode(documentId, stateMachineProperties);
			_dataModelNodeList.Remove(stateMachineNode.DataModel);

			stateMachine = stateMachineNode;
			RegisterEntity(stateMachine);
		}

		protected override void Build(ref IState state, ref State stateProperties)
		{
			var documentId = NewDocumentId();

			CounterBefore(inParallel: false, out var saved);
			base.Build(ref state, ref stateProperties);
			CounterAfter(saved);

			StateNode newState;

			if (stateProperties.States?.Count > 0)
			{
				if (stateProperties.Initial == null)
				{
					var initialDocId = NewDocumentIdAfter(documentId);
					var target = new[] { stateProperties.States[index: 0].As<StateEntityNode>() };
					var transition = new TransitionNode(NewDocumentIdAfter(initialDocId), transition: default, target);
					stateProperties.Initial = new InitialNode(initialDocId, transition);
				}

				newState = new CompoundNode(documentId, stateProperties);
			}
			else
			{
				newState = new StateNode(documentId, stateProperties);
			}

			if (newState.Id != null)
			{
				_idMap.Add(newState.Id, newState);
			}

			state = newState;
			RegisterEntity(state);
		}

		protected override void Build(ref IParallel parallel, ref Parallel parallelProperties)
		{
			var documentId = NewDocumentId();

			CounterBefore(inParallel: false, out var saved);
			base.Build(ref parallel, ref parallelProperties);
			CounterAfter(saved);

			var newParallel = new ParallelNode(documentId, parallelProperties);
			if (newParallel.Id != null)
			{
				_idMap.Add(newParallel.Id, newParallel);
			}

			parallel = newParallel;
			RegisterEntity(parallel);
		}

		protected override void Build(ref IFinal final, ref Final finalProperties)
		{
			var documentId = NewDocumentId();

			CounterBefore(inParallel: false, out var saved);
			base.Build(ref final, ref finalProperties);
			CounterAfter(saved);

			var newFinal = new FinalNode(documentId, finalProperties);
			if (newFinal.Id != null)
			{
				_idMap.Add(newFinal.Id, newFinal);
			}

			final = newFinal;
			RegisterEntity(final);
		}

		protected override void Build(ref IHistory history, ref History historyProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref history, ref historyProperties);

			var newHistory = new HistoryNode(documentId, historyProperties);
			if (newHistory.Id != null)
			{
				_idMap.Add(newHistory.Id, newHistory);
			}

			history = newHistory;
			RegisterEntity(history);
		}

		protected override void Build(ref IInitial initial, ref Initial initialProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref initial, ref initialProperties);

			initial = new InitialNode(documentId, initialProperties);
			RegisterEntity(initial);
		}

		protected override void Build(ref ITransition transition, ref Transition transitionProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref transition, ref transitionProperties);

			var newTransition = new TransitionNode(documentId, transitionProperties);

			if (transitionProperties.Target != null)
			{
				_targetMap.Add(newTransition);
			}

			transition = newTransition;
			RegisterEntity(transition);
		}

		protected override void Build(ref IOnEntry onEntry, ref OnEntry onEntryProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref onEntry, ref onEntryProperties);

			onEntry = new OnEntryNode(documentId, onEntryProperties);
			RegisterEntity(onEntry);
		}

		protected override void Build(ref IOnExit onExit, ref OnExit onExitProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref onExit, ref onExitProperties);

			onExit = new OnExitNode(documentId, onExitProperties);
			RegisterEntity(onExit);
		}

		protected override void Build(ref IDataModel dataModel, ref DataModel dataModelProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref dataModel, ref dataModelProperties);

			var dataModelNode = new DataModelNode(documentId, dataModelProperties);

			_dataModelNodeList.Add(dataModelNode);

			dataModel = dataModelNode;
			RegisterEntity(dataModel);
		}

		protected override void Build(ref IData data, ref Data dataProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref data, ref dataProperties);

			data = new DataNode(documentId, dataProperties);
			RegisterEntity(data);
		}

		protected override void Build(ref IInvoke invoke, ref Invoke invokeProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref invoke, ref invokeProperties);

			invoke = new InvokeNode(documentId, invokeProperties);
			RegisterEntity(invoke);
		}

		protected override void Build(ref IScriptExpression scriptExpression, ref ScriptExpression scriptExpressionProperties)
		{
			NewDocumentId();

			base.Build(ref scriptExpression, ref scriptExpressionProperties);

			scriptExpression = new ScriptExpressionNode(scriptExpressionProperties);
			RegisterEntity(scriptExpression);
		}

		protected override void Build(ref IExternalScriptExpression externalScriptExpression, ref ExternalScriptExpression externalScriptExpressionProperties)
		{
			NewDocumentId();

			base.Build(ref externalScriptExpression, ref externalScriptExpressionProperties);

			externalScriptExpression = new ExternalScriptExpressionNode(externalScriptExpressionProperties);
			RegisterEntity(externalScriptExpression);

			if (externalScriptExpression.Is<IExternalScriptConsumer>(out var consumer))
			{
				if (externalScriptExpression.Is<IExternalScriptProvider>(out var provider))
				{
					consumer.SetContent(provider.Content);
				}
				else
				{
					if (_externalScriptList == null)
					{
						_externalScriptList = new List<(Uri Uri, IExternalScriptConsumer Consumer)>();
					}

					_externalScriptList.Add((externalScriptExpressionProperties.Uri, consumer));
				}
			}
		}

		protected override void Build(ref ICustomAction customAction, ref CustomAction customActionProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref customAction, ref customActionProperties);

			customAction = new CustomActionNode(documentId, customActionProperties);
			RegisterEntity(customAction);

			if (customAction.Is<ICustomActionConsumer>(out var consumer))
			{
				consumer.SetAction(FindAction(customAction.Xml));
			}
		}

		private Func<IExecutionContext, CancellationToken, ValueTask> FindAction(string xml)
		{
			if (_customActionProviders != null)
			{
				using var stringReader = new StringReader(xml);
				using var xmlReader = XmlReader.Create(stringReader);

				xmlReader.MoveToContent();

				var namespaceUri = xmlReader.NamespaceURI;
				var name = xmlReader.LocalName;

				foreach (var handler in _customActionProviders)
				{
					if (handler.CanHandle(namespaceUri, name))
					{
						return handler.GetAction(xml);
					}
				}
			}

			return (context, token) => throw new NotSupportedException("Custom action does not supported");
		}

		protected override void Build(ref IExternalDataExpression externalDataExpression, ref ExternalDataExpression externalDataExpressionProperties)
		{
			NewDocumentId();

			base.Build(ref externalDataExpression, ref externalDataExpressionProperties);

			externalDataExpression = new ExternalDataExpressionNode(externalDataExpressionProperties);
			RegisterEntity(externalDataExpression);
		}

		protected override void Build(ref IValueExpression valueExpression, ref ValueExpression valueExpressionProperties)
		{
			NewDocumentId();

			base.Build(ref valueExpression, ref valueExpressionProperties);

			valueExpression = new ValueExpressionNode(valueExpressionProperties);
			RegisterEntity(valueExpression);
		}

		protected override void Build(ref ILocationExpression locationExpression, ref LocationExpression locationExpressionProperties)
		{
			NewDocumentId();

			base.Build(ref locationExpression, ref locationExpressionProperties);

			locationExpression = new LocationExpressionNode(locationExpressionProperties);
			RegisterEntity(locationExpression);
		}

		protected override void Build(ref IConditionExpression conditionExpression, ref ConditionExpression conditionExpressionProperties)
		{
			NewDocumentId();

			base.Build(ref conditionExpression, ref conditionExpressionProperties);

			conditionExpression = new ConditionExpressionNode(conditionExpressionProperties);
			RegisterEntity(conditionExpression);
		}

		protected override void Build(ref IDoneData doneData, ref DoneData doneDataProperties)
		{
			NewDocumentId();

			base.Build(ref doneData, ref doneDataProperties);

			doneData = new DoneDataNode(doneDataProperties);
			RegisterEntity(doneData);
		}

		protected override void Build(ref IContent content, ref Content contentProperties)
		{
			NewDocumentId();

			base.Build(ref content, ref contentProperties);

			content = new ContentNode(contentProperties);
			RegisterEntity(content);
		}

		protected override void Build(ref IFinalize finalize, ref Finalize finalizeProperties)
		{
			NewDocumentId();

			base.Build(ref finalize, ref finalizeProperties);

			finalize = new FinalizeNode(finalizeProperties);
			RegisterEntity(finalize);
		}

		protected override void Build(ref IParam param, ref Param paramProperties)
		{
			NewDocumentId();

			var documentId = NewDocumentId();

			base.Build(ref param, ref paramProperties);

			param = new ParamNode(documentId, paramProperties);
			RegisterEntity(param);
		}

		protected override void Visit(ref IIdentifier entity)
		{
			NewDocumentId();

			base.Visit(ref entity);

			entity = new IdentifierNode(entity);
			RegisterEntity(entity);
		}

		protected override void Visit(ref IOutgoingEvent entity)
		{
			NewDocumentId();

			base.Visit(ref entity);

			entity = new EventNode(entity);
			RegisterEntity(entity);
		}

		protected override void Visit(ref IEventDescriptor entity)
		{
			NewDocumentId();

			base.Visit(ref entity);

			entity = new EventDescriptorNode(entity);
			RegisterEntity(entity);
		}

		protected override void Build(ref ICancel cancel, ref Cancel cancelProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref cancel, ref cancelProperties);

			cancel = new CancelNode(documentId, cancelProperties);
			RegisterEntity(cancel);
		}

		protected override void Build(ref IAssign assign, ref Assign assignProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref assign, ref assignProperties);

			assign = new AssignNode(documentId, assignProperties);
			RegisterEntity(assign);
		}

		protected override void Build(ref IForEach forEach, ref ForEach forEachProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref forEach, ref forEachProperties);

			forEach = new ForEachNode(documentId, forEachProperties);
			RegisterEntity(forEachProperties);
		}

		protected override void Build(ref IIf @if, ref If ifProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref @if, ref ifProperties);

			@if = new IfNode(documentId, ifProperties);
			RegisterEntity(@if);
		}

		protected override void Build(ref IElseIf elseIf, ref ElseIf elseIfProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref elseIf, ref elseIfProperties);

			elseIf = new ElseIfNode(documentId, elseIfProperties);
			RegisterEntity(elseIf);
		}

		protected override void Build(ref IElse @else, ref Else elseProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref @else, ref elseProperties);

			@else = new ElseNode(documentId, elseProperties);
			RegisterEntity(@else);
		}

		protected override void Build(ref ILog log, ref Log logProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref log, ref logProperties);

			log = new LogNode(documentId, logProperties);
			RegisterEntity(log);
		}

		protected override void Build(ref IRaise raise, ref Raise raiseProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref raise, ref raiseProperties);

			raise = new RaiseNode(documentId, raiseProperties);
			RegisterEntity(raise);
		}

		protected override void Build(ref ISend send, ref Send sendProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref send, ref sendProperties);

			send = new SendNode(documentId, sendProperties);
			RegisterEntity(send);
		}

		protected override void Build(ref IScript script, ref Script scriptProperties)
		{
			var documentId = NewDocumentId();

			base.Build(ref script, ref scriptProperties);

			script = new ScriptNode(documentId, scriptProperties);
			RegisterEntity(script);
		}

		protected override void VisitUnknown(ref IExecutableEntity entity)
		{
			var documentId = NewDocumentId();

			base.VisitUnknown(ref entity);

			if (!entity.Is<IStoreSupport>())
			{
				entity = new RuntimeExecNode(documentId, entity);
				RegisterEntity(entity);
			}
		}

		protected override void Visit(ref IExecutableEntity executableEntity)
		{
			if (_deepLevel == 0)
			{
				_dataModelHandler.Process(ref executableEntity);
			}

			_deepLevel ++;
			base.Visit(ref executableEntity);
			_deepLevel --;
		}

		protected override void Visit(ref IDataModel dataModel)
		{
			if (_deepLevel == 0)
			{
				_dataModelHandler.Process(ref dataModel);
			}

			_deepLevel ++;
			base.Visit(ref dataModel);
			_deepLevel --;
		}

		protected override void Visit(ref IDoneData doneData)
		{
			if (_deepLevel == 0)
			{
				_dataModelHandler.Process(ref doneData);
			}

			_deepLevel ++;
			base.Visit(ref doneData);
			_deepLevel --;
		}

		protected override void Visit(ref IInvoke invoke)
		{
			if (_deepLevel == 0)
			{
				_dataModelHandler.Process(ref invoke);
			}

			_deepLevel ++;
			base.Visit(ref invoke);
			_deepLevel --;
		}
	}
}