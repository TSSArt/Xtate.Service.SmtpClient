using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public abstract class StateEntityNode : IStateEntity, IStoreSupport, IDocumentId
	{
		public static readonly IComparer<StateEntityNode> EntryOrder = new DocumentOrderComparer(reverseOrder: false);
		public static readonly IComparer<StateEntityNode> ExitOrder  = new DocumentOrderComparer(reverseOrder: true);

		private readonly LinkedListNode<int> _documentIdNode;

		protected StateEntityNode(LinkedListNode<int> documentIdNode, IEnumerable<StateEntityNode> children)
		{
			_documentIdNode = documentIdNode;

			if (children != null)
			{
				foreach (var stateEntity in children)
				{
					stateEntity.Parent = this;
				}
			}
		}

		public StateEntityNode Parent { get; private set; }

		public virtual bool                           IsAtomicState => throw GetNotSupportedException();
		public virtual IIdentifier                    Id            => throw GetNotSupportedException();
		public virtual ImmutableArray<TransitionNode>  Transitions   => throw GetNotSupportedException();
		public virtual ImmutableArray<OnEntryNode>     OnEntry       => throw GetNotSupportedException();
		public virtual ImmutableArray<OnExitNode>      OnExit        => throw GetNotSupportedException();
		public virtual ImmutableArray<InvokeNode>      Invoke        => throw GetNotSupportedException();
		public virtual ImmutableArray<StateEntityNode> States        => throw GetNotSupportedException();
		public virtual ImmutableArray<HistoryNode>     HistoryStates => throw GetNotSupportedException();
		public virtual DataModelNode                  DataModel     => throw GetNotSupportedException();

		public int DocumentId => _documentIdNode.Value;

		void IStoreSupport.Store(Bucket bucket) => Store(bucket);

		private NotSupportedException GetNotSupportedException() => new NotSupportedException("Specified method is not supported in type " + GetType().Name);

		protected static IEnumerable<StateEntityNode> GetChildNodes(IInitial initial, ImmutableArray<IStateEntity> states, ImmutableArray<IHistory> historyStates = null)
		{
			var initialNode = initial.As<InitialNode>();

			if (initialNode != null)
			{
				yield return initialNode;
			}

			if (states != null)
			{
				foreach (var node in states.AsEnumerableOf<StateEntityNode>())
				{
					yield return node;
				}
			}

			if (historyStates != null)
			{
				foreach (var node in historyStates.AsEnumerableOf<StateEntityNode>())
				{
					yield return node;
				}
			}
		}

		protected abstract void Store(Bucket bucket);

		private sealed class DocumentOrderComparer : IComparer<StateEntityNode>
		{
			private readonly bool _reverseOrder;

			public DocumentOrderComparer(bool reverseOrder) => _reverseOrder = reverseOrder;

			public int Compare(StateEntityNode x, StateEntityNode y) => _reverseOrder ? InternalCompare(y, x) : InternalCompare(x, y);

			private static int InternalCompare(StateEntityNode x, StateEntityNode y)
			{
				if (x == y) return 0;
				if (y == null) return 1;
				if (x == null) return -1;

				return x.DocumentId.CompareTo(y.DocumentId);
			}
		}
	}
}