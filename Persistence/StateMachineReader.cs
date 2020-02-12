using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class StateMachineReader
	{
		private Dictionary<int, IEntity> _forwardEntities;

		public IStateMachine Build(Bucket bucket, Dictionary<int, IEntity> forwardEntities = null)
		{
			_forwardEntities = forwardEntities;

			return RestoreStateMachine(bucket);
		}

		private static bool Exist(Bucket bucket, TypeInfo typeInfo)
		{
			if (bucket.TryGet(Key.TypeInfo, out TypeInfo storedTypeInfo))
			{
				if (storedTypeInfo != typeInfo)
				{
					throw new ArgumentException("Unexpected TypeInfo value");
				}

				return true;
			}

			return false;
		}

		private IExecutableEntity ForwardExecEntity(Bucket bucket)
		{
			var documentId = bucket.GetInt32(Key.DocumentId);

			if (_forwardEntities == null)
			{
				throw new InvalidOperationException("Forward entities required to restore state machine");
			}

			if (!_forwardEntities.TryGetValue(documentId, out var entity))
			{
				throw new InvalidOperationException("Forward entity can not be found");
			}

			if (!(entity is IExecutableEntity executableEntity))
			{
				throw new InvalidOperationException("Forward entity has incorrect type");
			}

			return executableEntity;
		}

		private IStateMachine RestoreStateMachine(Bucket bucket) =>
				Exist(bucket, TypeInfo.StateMachineNode)
						? new StateMachine
						  {
								  Ancestor = new EntityData(bucket),
								  Name = bucket.GetString(Key.Name),
								  DataModelType = bucket.GetString(Key.DataModelType),
								  Binding = bucket.Get<BindingType>(Key.Binding),
								  Script = RestoreScript(bucket.Nested(Key.Script)),
								  DataModel = RestoreDataModel(bucket.Nested(Key.DataModel)),
								  Initial = RestoreInitial(bucket.Nested(Key.Initial)),
								  States = bucket.RestoreList(Key.States, RestoreStateEntity),
								  Options = bucket.RestoreDictionary(Key.Options)
						  }
						: (IStateMachine) null;

		private static IDataModel RestoreDataModel(Bucket bucket) =>
				Exist(bucket, TypeInfo.DataModelNode)
						? new DataModel
						  {
								  Ancestor = new EntityData(bucket),
								  Data = bucket.RestoreList(Key.DataList, RestoreData)
						  }
						: (IDataModel) null;

		private IInitial RestoreInitial(Bucket bucket) =>
				Exist(bucket, TypeInfo.InitialNode)
						? new Initial
						  {
								  Ancestor = new EntityData(bucket),
								  Transition = RestoreTransition(bucket.Nested(Key.Transition))
						  }
						: (IInitial) null;

		private ITransition RestoreTransition(Bucket bucket) =>
				Exist(bucket, TypeInfo.TransitionNode)
						? new Transition
						  {
								  Ancestor = new EntityData(bucket),
								  Event = bucket.RestoreList(Key.Event, RestoreEventDescriptor),
								  Condition = RestoreCondition(bucket.Nested(Key.Condition)),
								  Target = bucket.RestoreList(Key.Target, RestoreIdentifier),
								  Type = bucket.Get<TransitionType>(Key.TransitionType),
								  Action = bucket.RestoreList(Key.Action, RestoreExecutableEntity)
						  }
						: (ITransition) null;

		private static IAssign RestoreAssign(Bucket bucket) =>
				Exist(bucket, TypeInfo.AssignNode)
						? new Assign
						  {
								  Ancestor = new EntityData(bucket),
								  Location = RestoreLocationExpression(bucket.Nested(Key.Location)),
								  Expression = RestoreValueExpression(bucket.Nested(Key.Expression)),
								  InlineContent = bucket.GetString(Key.InlineContent)
						  }
						: (IAssign) null;

		private static ICancel RestoreCancel(Bucket bucket) =>
				Exist(bucket, TypeInfo.CancelNode)
						? new Cancel
						  {
								  Ancestor = new EntityData(bucket),
								  SendId = bucket.GetString(Key.SendId),
								  SendIdExpression = RestoreValueExpression(bucket.Nested(Key.SendIdExpression))
						  }
						: (ICancel) null;

		private IState RestoreCompound(Bucket bucket) =>
				Exist(bucket, TypeInfo.CompoundNode)
						? new State
						  {
								  Ancestor = new EntityData(bucket),
								  Id = RestoreIdentifier(bucket.Nested(Key.Id)),
								  Initial = RestoreInitial(bucket.Nested(Key.Initial)),
								  DataModel = RestoreDataModel(bucket.Nested(Key.DataModel)),
								  States = bucket.RestoreList(Key.States, RestoreStateEntity),
								  HistoryStates = bucket.RestoreList(Key.HistoryStates, RestoreHistory),
								  Transitions = bucket.RestoreList(Key.Transitions, RestoreTransition),
								  OnEntry = bucket.RestoreList(Key.OnEntry, RestoreOnEntry),
								  OnExit = bucket.RestoreList(Key.OnExit, RestoreOnExit),
								  Invoke = bucket.RestoreList(Key.Invoke, RestoreInvoke)
						  }
						: (IState) null;

		private IExecutableEntity RestoreCondition(Bucket bucket)
		{
			if (!bucket.TryGet(Key.TypeInfo, out TypeInfo typeInfo))
			{
				return null;
			}

			switch (typeInfo)
			{
				case TypeInfo.ConditionExpressionNode: return RestoreConditionExpression(bucket);
				case TypeInfo.RuntimeExecNode: return ForwardExecEntity(bucket);
				default: throw new ArgumentOutOfRangeException(nameof(typeInfo), typeInfo, message: "Unknown Condition type");
			}
		}

		private static IConditionExpression RestoreConditionExpression(Bucket bucket) =>
				Exist(bucket, TypeInfo.ConditionExpressionNode)
						? new ConditionExpression
						  {
								  Ancestor = new EntityData(bucket),
								  Expression = bucket.GetString(Key.Expression)
						  }
						: (IConditionExpression) null;

		private static IContent RestoreContent(Bucket bucket) =>
				Exist(bucket, TypeInfo.ContentNode)
						? new Content
						  {
								  Ancestor = new EntityData(bucket),
								  Expression = RestoreValueExpression(bucket.Nested(Key.Expression)),
								  Body = bucket.TryGet(Key.Body, out string body) ? new ContentBody { Value = body } : (IContentBody) null
						  }
						: (IContent) null;

		private static IData RestoreData(Bucket bucket) =>
				Exist(bucket, TypeInfo.DataNode)
						? new Data
						  {
								  Ancestor = new EntityData(bucket),
								  Id = bucket.GetString(Key.Id),
								  Source = RestoreExternalDataExpression(bucket.Nested(Key.Source)),
								  Expression = RestoreValueExpression(bucket.Nested(Key.Expression)),
								  InlineContent = bucket.GetString(Key.InlineContent)
						  }
						: (IData) null;

		private static IDoneData RestoreDoneData(Bucket bucket) =>
				Exist(bucket, TypeInfo.DoneDataNode)
						? new DoneData
						  {
								  Ancestor = new EntityData(bucket),
								  Content = RestoreContent(bucket.Nested(Key.Source)),
								  Parameters = bucket.RestoreList(Key.Parameters, RestoreParam)
						  }
						: (IDoneData) null;

		private static IElseIf RestoreElseIf(Bucket bucket) =>
				Exist(bucket, TypeInfo.ElseIfNode)
						? new ElseIf
						  {
								  Ancestor = new EntityData(bucket),
								  Condition = RestoreConditionExpression(bucket.Nested(Key.Condition))
						  }
						: (IElseIf) null;

		private static IElse RestoreElse(Bucket bucket) =>
				Exist(bucket, TypeInfo.ElseNode)
						? new Else
						  {
								  Ancestor = new EntityData(bucket)
						  }
						: (IElse) null;

		private static IEventDescriptor RestoreEventDescriptor(Bucket bucket) => (EventDescriptor) bucket.GetString(Key.Id);

		private static IOutgoingEvent RestoreEvent(Bucket bucket) => new Event(bucket.GetString(Key.Id)) { Target = Event.InternalTarget };

		private IExecutableEntity RestoreExecutableEntity(Bucket bucket)
		{
			var typeInfo = bucket.Get<TypeInfo>(Key.TypeInfo);
			switch (typeInfo)
			{
				case TypeInfo.AssignNode: return RestoreAssign(bucket);
				case TypeInfo.CancelNode: return RestoreCancel(bucket);
				case TypeInfo.CustomActionNode: return RestoreCustomAction(bucket);
				case TypeInfo.ForEachNode: return RestoreForEach(bucket);
				case TypeInfo.IfNode: return RestoreIf(bucket);
				case TypeInfo.ElseIfNode: return RestoreElseIf(bucket);
				case TypeInfo.ElseNode: return RestoreElse(bucket);
				case TypeInfo.LogNode: return RestoreLog(bucket);
				case TypeInfo.RaiseNode: return RestoreRaise(bucket);
				case TypeInfo.ScriptNode: return RestoreScript(bucket);
				case TypeInfo.SendNode: return RestoreSend(bucket);
				case TypeInfo.RuntimeExecNode: return ForwardExecEntity(bucket);
				default: throw new ArgumentOutOfRangeException(nameof(typeInfo), typeInfo, message: "Unknown Executable Entity type");
			}
		}

		private static ILog RestoreLog(Bucket bucket) =>
				Exist(bucket, TypeInfo.LogNode)
						? new Log
						  {
								  Ancestor = new EntityData(bucket),
								  Label = bucket.GetString(Key.Label),
								  Expression = RestoreValueExpression(bucket.Nested(Key.Expression))
						  }
						: (ILog) null;

		private static IRaise RestoreRaise(Bucket bucket) =>
				Exist(bucket, TypeInfo.RaiseNode)
						? new Raise
						  {
								  Ancestor = new EntityData(bucket),
								  Event = RestoreEvent(bucket.Nested(Key.Event))
						  }
						: (IRaise) null;

		private static IScript RestoreScript(Bucket bucket) =>
				Exist(bucket, TypeInfo.ScriptNode)
						? new Script
						  {
								  Ancestor = new EntityData(bucket),
								  Source = RestoreExternalScriptExpression(bucket.Nested(Key.Source)),
								  Content = RestoreScriptExpression(bucket.Nested(Key.Content))
						  }
						: (IScript) null;

		private static ICustomAction RestoreCustomAction(Bucket bucket) =>
				Exist(bucket, TypeInfo.CustomActionNode)
						? new CustomAction
						  {
								  Ancestor = new EntityData(bucket),
								  Xml = bucket.GetString(Key.Content)
						  }
						: (ICustomAction) null;

		private static ISend RestoreSend(Bucket bucket) =>
				Exist(bucket, TypeInfo.SendNode)
						? new Send
						  {
								  Ancestor = new EntityData(bucket),
								  Id = bucket.GetString(Key.Id),
								  Type = bucket.GetUri(Key.Type),
								  Event = bucket.GetString(Key.Event),
								  Target = bucket.GetUri(Key.Target),
								  DelayMs = bucket.GetInt32(Key.DelayMs),
								  TypeExpression = RestoreValueExpression(bucket.Nested(Key.TypeExpression)),
								  EventExpression = RestoreValueExpression(bucket.Nested(Key.EventExpression)),
								  TargetExpression = RestoreValueExpression(bucket.Nested(Key.TargetExpression)),
								  DelayExpression = RestoreValueExpression(bucket.Nested(Key.DelayExpression)),
								  IdLocation = RestoreLocationExpression(bucket.Nested(Key.IdLocation)),
								  NameList = bucket.RestoreList(Key.NameList, RestoreLocationExpression),
								  Parameters = bucket.RestoreList(Key.Parameters, RestoreParam),
								  Content = RestoreContent(bucket.Nested(Key.Content))
						  }
						: (ISend) null;

		private static IExternalDataExpression RestoreExternalDataExpression(Bucket bucket) =>
				Exist(bucket, TypeInfo.ExternalDataExpressionNode)
						? new ExternalDataExpression
						  {
								  Ancestor = new EntityData(bucket),
								  Uri = bucket.GetUri(Key.Uri)
						  }
						: (IExternalDataExpression) null;

		private static IExternalScriptExpression RestoreExternalScriptExpression(Bucket bucket)
		{
			if (!Exist(bucket, TypeInfo.ExternalScriptExpressionNode))
			{
				return null;
			}

			var content = bucket.GetString(Key.Content);
			if (content == null)
			{
				return new ExternalScriptExpression
					   {
							   Ancestor = new EntityData(bucket),
							   Uri = bucket.GetUri(Key.Uri)
					   };
			}

			return new ExternalScriptExpressionWithContent(new EntityData(bucket), bucket.GetUri(Key.Uri), content);
		}

		private IFinalize RestoreFinalize(Bucket bucket) =>
				Exist(bucket, TypeInfo.FinalizeNode)
						? new Finalize
						  {
								  Ancestor = new EntityData(bucket),
								  Action = bucket.RestoreList(Key.Parameters, RestoreExecutableEntity)
						  }
						: (IFinalize) null;

		private IFinal RestoreFinal(Bucket bucket) =>
				Exist(bucket, TypeInfo.FinalNode)
						? new Final
						  {
								  Ancestor = new EntityData(bucket),
								  Id = RestoreIdentifier(bucket.Nested(Key.Id)),
								  OnEntry = bucket.RestoreList(Key.OnEntry, RestoreOnEntry),
								  OnExit = bucket.RestoreList(Key.OnExit, RestoreOnExit),
								  DoneData = RestoreDoneData(bucket.Nested(Key.DoneData))
						  }
						: (IFinal) null;

		private IForEach RestoreForEach(Bucket bucket) =>
				Exist(bucket, TypeInfo.ForEachNode)
						? new ForEach
						  {
								  Ancestor = new EntityData(bucket),
								  Array = RestoreValueExpression(bucket.Nested(Key.Array)),
								  Item = RestoreLocationExpression(bucket.Nested(Key.Item)),
								  Index = RestoreLocationExpression(bucket.Nested(Key.Index)),
								  Action = bucket.RestoreList(Key.Action, RestoreExecutableEntity)
						  }
						: (IForEach) null;

		private IHistory RestoreHistory(Bucket bucket) =>
				Exist(bucket, TypeInfo.HistoryNode)
						? new History
						  {
								  Ancestor = new EntityData(bucket),
								  Id = RestoreIdentifier(bucket.Nested(Key.Id)),
								  Type = bucket.Get<HistoryType>(Key.HistoryType),
								  Transition = RestoreTransition(bucket.Nested(Key.Transition))
						  }
						: (IHistory) null;

		private static IIdentifier RestoreIdentifier(Bucket bucket)
		{
			var id = bucket.GetString(Key.Id);
			return id != null ? (Identifier) id : null;
		}

		private IIf RestoreIf(Bucket bucket) =>
				Exist(bucket, TypeInfo.IfNode)
						? new If
						  {
								  Ancestor = new EntityData(bucket),
								  Condition = RestoreConditionExpression(bucket.Nested(Key.Condition)),
								  Action = bucket.RestoreList(Key.Action, RestoreExecutableEntity)
						  }
						: (IIf) null;

		private IInvoke RestoreInvoke(Bucket bucket) =>
				Exist(bucket, TypeInfo.InvokeNode)
						? new Invoke
						  {
								  Ancestor = new EntityData(bucket),
								  Id = bucket.GetString(Key.Id),
								  Type = bucket.GetUri(Key.Type),
								  Source = bucket.GetUri(Key.Source),
								  AutoForward = bucket.GetBoolean(Key.AutoForward),
								  TypeExpression = RestoreValueExpression(bucket.Nested(Key.TypeExpression)),
								  SourceExpression = RestoreValueExpression(bucket.Nested(Key.SourceExpression)),
								  IdLocation = RestoreLocationExpression(bucket.Nested(Key.IdLocation)),
								  NameList = bucket.RestoreList(Key.NameList, RestoreLocationExpression),
								  Parameters = bucket.RestoreList(Key.Parameters, RestoreParam),
								  Finalize = RestoreFinalize(bucket.Nested(Key.Finalize)),
								  Content = RestoreContent(bucket.Nested(Key.Content))
						  }
						: (IInvoke) null;

		private static ILocationExpression RestoreLocationExpression(Bucket bucket) =>
				Exist(bucket, TypeInfo.LocationExpressionNode)
						? new LocationExpression
						  {
								  Ancestor = new EntityData(bucket),
								  Expression = bucket.GetString(Key.Expression)
						  }
						: (ILocationExpression) null;

		private IOnEntry RestoreOnEntry(Bucket bucket) =>
				Exist(bucket, TypeInfo.OnEntryNode)
						? new OnEntry
						  {
								  Ancestor = new EntityData(bucket),
								  Action = bucket.RestoreList(Key.Action, RestoreExecutableEntity)
						  }
						: (IOnEntry) null;

		private IOnExit RestoreOnExit(Bucket bucket) =>
				Exist(bucket, TypeInfo.OnExitNode)
						? new OnExit
						  {
								  Ancestor = new EntityData(bucket),
								  Action = bucket.RestoreList(Key.Action, RestoreExecutableEntity)
						  }
						: (IOnExit) null;

		private IParallel RestoreParallel(Bucket bucket) =>
				Exist(bucket, TypeInfo.ParallelNode)
						? new Parallel
						  {
								  Ancestor = new EntityData(bucket),
								  Id = RestoreIdentifier(bucket.Nested(Key.Id)),
								  DataModel = RestoreDataModel(bucket.Nested(Key.DataModel)),
								  States = bucket.RestoreList(Key.States, RestoreStateEntity),
								  HistoryStates = bucket.RestoreList(Key.HistoryStates, RestoreHistory),
								  Transitions = bucket.RestoreList(Key.Transitions, RestoreTransition),
								  OnEntry = bucket.RestoreList(Key.OnEntry, RestoreOnEntry),
								  OnExit = bucket.RestoreList(Key.OnExit, RestoreOnExit),
								  Invoke = bucket.RestoreList(Key.Invoke, RestoreInvoke)
						  }
						: (IParallel) null;

		private static IParam RestoreParam(Bucket bucket) =>
				Exist(bucket, TypeInfo.ParamNode)
						? new Param
						  {
								  Ancestor = new EntityData(bucket),
								  Name = bucket.GetString(Key.Name),
								  Expression = RestoreValueExpression(bucket.Nested(Key.Expression)),
								  Location = RestoreLocationExpression(bucket.Nested(Key.Location))
						  }
						: (IParam) null;

		private static IScriptExpression RestoreScriptExpression(Bucket bucket) =>
				Exist(bucket, TypeInfo.ScriptExpressionNode)
						? new ScriptExpression
						  {
								  Ancestor = new EntityData(bucket),
								  Expression = bucket.GetString(Key.Expression)
						  }
						: (IScriptExpression) null;

		private IStateEntity RestoreStateEntity(Bucket bucket)
		{
			var typeInfo = bucket.Get<TypeInfo>(Key.TypeInfo);
			switch (typeInfo)
			{
				case TypeInfo.CompoundNode: return RestoreCompound(bucket);
				case TypeInfo.FinalNode: return RestoreFinal(bucket);
				case TypeInfo.ParallelNode: return RestoreParallel(bucket);
				case TypeInfo.StateNode: return RestoreState(bucket);
				default: throw new ArgumentOutOfRangeException(nameof(typeInfo), typeInfo, message: "Unknown State Entity type");
			}
		}

		private IState RestoreState(Bucket bucket) =>
				Exist(bucket, TypeInfo.StateNode)
						? new State
						  {
								  Ancestor = new EntityData(bucket),
								  Id = RestoreIdentifier(bucket.Nested(Key.Id)),
								  Initial = RestoreInitial(bucket.Nested(Key.Initial)),
								  DataModel = RestoreDataModel(bucket.Nested(Key.DataModel)),
								  States = bucket.RestoreList(Key.States, RestoreStateEntity),
								  HistoryStates = bucket.RestoreList(Key.HistoryStates, RestoreHistory),
								  Transitions = bucket.RestoreList(Key.Transitions, RestoreTransition),
								  OnEntry = bucket.RestoreList(Key.OnEntry, RestoreOnEntry),
								  OnExit = bucket.RestoreList(Key.OnExit, RestoreOnExit),
								  Invoke = bucket.RestoreList(Key.Invoke, RestoreInvoke)
						  }
						: (IState) null;

		private static IValueExpression RestoreValueExpression(Bucket bucket) =>
				Exist(bucket, TypeInfo.ValueExpressionNode)
						? new ValueExpression
						  {
								  Ancestor = new EntityData(bucket),
								  Expression = bucket.GetString(Key.Expression)
						  }
						: (IValueExpression) null;

		private class EntityData : IPersistedDocumentId
		{
			public EntityData(Bucket bucket)
			{
				if (bucket.TryGet(Key.DocumentId, out int documentId))
				{
					DocumentId = documentId;
				}
			}

			public int DocumentId { get; }
		}

		private class ExternalScriptExpressionWithContent : IExternalScriptExpression, IExternalScriptProvider, IAncestorProvider
		{
			public ExternalScriptExpressionWithContent(object ancestor, Uri uri, string content)
			{
				Ancestor = ancestor;
				Uri = uri;
				Content = content;
			}

			public object Ancestor { get; }

			public Uri Uri { get; }

			public string Content { get; }
		}
	}
}