using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class StateNode : StateEntityNode, IState, IAncestorProvider, IDebugEntityId
	{
		private readonly State _state;

		public StateNode(LinkedListNode<int> documentIdNode, in State state) : base(documentIdNode, GetChildNodes(state.Initial, state.States, state.HistoryStates))
		{
			_state = state;

			var id = state.Id ?? new IdentifierNode(new RuntimeIdentifier());
			var transitions = state.Transitions.AsListOf<TransitionNode>() ?? Array.Empty<TransitionNode>();
			var invokeList = state.Invoke.AsListOf<InvokeNode>() ?? Array.Empty<InvokeNode>();

			Id = id;
			States = state.States.AsListOf<StateEntityNode>();
			HistoryStates = state.HistoryStates.AsListOf<HistoryNode>() ?? Array.Empty<HistoryNode>();
			Transitions = transitions;
			OnEntry = state.OnEntry.AsListOf<OnEntryNode>() ?? Array.Empty<OnEntryNode>();
			OnExit = state.OnExit.AsListOf<OnExitNode>() ?? Array.Empty<OnExitNode>();
			Invoke = invokeList;
			Initial = state.Initial.As<InitialNode>();
			DataModel = state.DataModel.As<DataModelNode>();

			foreach (var transition in transitions)
			{
				transition.SetSource(this);
			}

			foreach (var invoke in invokeList)
			{
				invoke.SetStateId(id);
			}
		}

		public override bool                           IsAtomicState => true;
		public override IReadOnlyList<InvokeNode>      Invoke        { get; }
		public override IReadOnlyList<TransitionNode>  Transitions   { get; }
		public override IReadOnlyList<HistoryNode>     HistoryStates { get; }
		public override IReadOnlyList<StateEntityNode> States        { get; }
		public override IReadOnlyList<OnEntryNode>     OnEntry       { get; }
		public override IReadOnlyList<OnExitNode>      OnExit        { get; }
		public override DataModelNode                  DataModel     { get; }

		protected InitialNode Initial { get; }

		object IAncestorProvider.Ancestor => _state.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}(#{DocumentId})";

		public override IIdentifier Id { get; }

		IInitial IState.                   Initial       => Initial;
		IDataModel IState.                 DataModel     => DataModel;
		IReadOnlyList<IInvoke> IState.     Invoke        => Invoke;
		IReadOnlyList<IStateEntity> IState.States        => States;
		IReadOnlyList<IHistory> IState.    HistoryStates => HistoryStates;
		IReadOnlyList<ITransition> IState. Transitions   => Transitions;
		IReadOnlyList<IOnEntry> IState.    OnEntry       => OnEntry;
		IReadOnlyList<IOnExit> IState.     OnExit        => OnExit;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.StateNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Id, Id);
			bucket.AddEntity(Key.Initial, Initial);
			bucket.AddEntity(Key.DataModel, DataModel);
			bucket.AddEntityList(Key.HistoryStates, HistoryStates);
			bucket.AddEntityList(Key.Transitions, Transitions);
			bucket.AddEntityList(Key.OnEntry, OnEntry);
			bucket.AddEntityList(Key.OnExit, OnExit);
			bucket.AddEntityList(Key.Invoke, Invoke);
		}
	}
}