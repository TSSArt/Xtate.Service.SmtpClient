using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class FinalNode : StateEntityNode, IFinal, IAncestorProvider, IDebugEntityId
	{
		private readonly Final _final;

		public FinalNode(LinkedListNode<int> documentIdNode, in Final final) : base(documentIdNode, children: null)
		{
			_final = final;

			Id = final.Id ?? new IdentifierNode(new RuntimeIdentifier());
			OnEntry = final.OnEntry.AsListOf<OnEntryNode>() ?? Array.Empty<OnEntryNode>();
			OnExit = final.OnExit.AsListOf<OnExitNode>() ?? Array.Empty<OnExitNode>();
			DoneData = final.DoneData.As<DoneDataNode>();
		}

		public override bool                          IsAtomicState => true;
		public override IReadOnlyList<TransitionNode> Transitions   => Array.Empty<TransitionNode>();
		public override IReadOnlyList<HistoryNode>    HistoryStates => Array.Empty<HistoryNode>();
		public override IReadOnlyList<InvokeNode>     Invoke        => Array.Empty<InvokeNode>();
		public override IReadOnlyList<OnEntryNode>    OnEntry       { get; }
		public override IReadOnlyList<OnExitNode>     OnExit        { get; }
		public          DoneDataNode                  DoneData      { get; }

		object IAncestorProvider.Ancestor => _final.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}(${DocumentId})";

		public override IIdentifier Id { get; }

		IReadOnlyList<IOnEntry> IFinal.OnEntry  => OnEntry;
		IReadOnlyList<IOnExit> IFinal. OnExit   => OnExit;
		IDoneData IFinal.              DoneData => DoneData;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.FinalNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Id, Id);
			bucket.AddEntityList(Key.OnEntry, OnEntry);
			bucket.AddEntityList(Key.OnExit, OnExit);
			bucket.AddEntity(Key.DoneData, DoneData);
		}
	}
}