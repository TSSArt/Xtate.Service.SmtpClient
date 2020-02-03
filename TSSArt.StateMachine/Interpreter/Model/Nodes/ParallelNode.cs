using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class ParallelNode : StateEntityNode, IParallel, IAncestorProvider, IDebugEntityId
	{
		private readonly Parallel _parallel;

		public ParallelNode(LinkedListNode<int> documentIdNode, in Parallel parallel) : base(documentIdNode, GetChildNodes(initial: null, parallel.States, parallel.HistoryStates))
		{
			_parallel = parallel;

			var id = parallel.Id ?? new IdentifierNode(new RuntimeIdentifier());
			var transitions = parallel.Transitions.AsListOf<TransitionNode>() ?? Array.Empty<TransitionNode>();
			var invokeList = parallel.Invoke.AsListOf<InvokeNode>() ?? Array.Empty<InvokeNode>();

			Id = id;
			States = parallel.States.AsListOf<StateEntityNode>();
			HistoryStates = parallel.HistoryStates.AsListOf<HistoryNode>() ?? Array.Empty<HistoryNode>();
			Transitions = transitions;
			OnEntry = parallel.OnEntry.AsListOf<OnEntryNode>() ?? Array.Empty<OnEntryNode>();
			OnExit = parallel.OnExit.AsListOf<OnExitNode>() ?? Array.Empty<OnExitNode>();
			Invoke = invokeList;
			DataModel = parallel.DataModel.As<DataModelNode>();

			foreach (var transition in transitions)
			{
				transition.SetSource(this);
			}

			foreach (var invoke in invokeList)
			{
				invoke.SetStateId(id);
			}
		}

		public override bool                           IsAtomicState => false;
		public override /**/ImmutableArray<InvokeNode>      Invoke        { get; }
		public override /**/ImmutableArray<TransitionNode>  Transitions   { get; }
		public override /**/ImmutableArray<HistoryNode>     HistoryStates { get; }
		public override /**/ImmutableArray<StateEntityNode> States        { get; }
		public override /**/ImmutableArray<OnEntryNode>     OnEntry       { get; }
		public override /**/ImmutableArray<OnExitNode>      OnExit        { get; }
		public override DataModelNode                  DataModel     { get; }

		object IAncestorProvider.Ancestor => _parallel.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}(#{DocumentId})";

		public override IIdentifier Id { get; }

		IDataModel IParallel.                 DataModel     => DataModel;
		/**/ImmutableArray<IInvoke> IParallel.     Invoke        => Invoke;
		/**/ImmutableArray<IStateEntity> IParallel.States        => States;
		/**/ImmutableArray<IHistory> IParallel.    HistoryStates => HistoryStates;
		/**/ImmutableArray<ITransition> IParallel. Transitions   => Transitions;
		/**/ImmutableArray<IOnEntry> IParallel.    OnEntry       => OnEntry;
		/**/ImmutableArray<IOnExit> IParallel.     OnExit        => OnExit;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ParallelNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Id, Id);
			bucket.AddEntity(Key.DataModel, DataModel);
			bucket.AddEntityList(Key.States, States);
			bucket.AddEntityList(Key.HistoryStates, HistoryStates);
			bucket.AddEntityList(Key.Transitions, Transitions);
			bucket.AddEntityList(Key.OnEntry, OnEntry);
			bucket.AddEntityList(Key.OnExit, OnExit);
			bucket.AddEntityList(Key.Invoke, Invoke);
		}
	}
}