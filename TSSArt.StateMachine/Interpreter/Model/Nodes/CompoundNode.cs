using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class CompoundNode : StateNode, IStoreSupport, IDebugEntityId
	{
		public CompoundNode(LinkedListNode<int> documentIdNode, in StateEntity state) : base(documentIdNode, state)
		{
			Infrastructure.Assert(base.Initial != null);
		}

		public new InitialNode Initial => base.Initial!;

		public override bool IsAtomicState => false;

		FormattableString IDebugEntityId.EntityId => @$"{Id}(#{DocumentId})";

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.CompoundNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Id, Id);
			bucket.AddEntity(Key.Initial, Initial);
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