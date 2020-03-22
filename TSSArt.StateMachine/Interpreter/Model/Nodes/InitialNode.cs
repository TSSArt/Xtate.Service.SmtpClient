using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class InitialNode : StateEntityNode, IInitial, IAncestorProvider, IDebugEntityId
	{
		private readonly InitialEntity _initial;

		public InitialNode(LinkedListNode<int> documentIdNode, in InitialEntity initial) : base(documentIdNode, children: null)
		{
			Infrastructure.Assert(initial.Transition != null);

			_initial = initial;
			Transition = initial.Transition.As<TransitionNode>();

			Transition.SetSource(this);
		}

		public InitialNode(LinkedListNode<int> documentIdNode, TransitionNode transition) : base(documentIdNode, children: null)
		{
			Transition = transition ?? throw new ArgumentNullException(nameof(transition));

			Transition.SetSource(this);
		}

		public TransitionNode Transition { get; }

		object? IAncestorProvider.Ancestor => _initial.Ancestor;

		public FormattableString EntityId => @$"(#{DocumentId})";

		ITransition IInitial.Transition => _initial.Transition!;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.InitialNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Transition, Transition);
		}
	}
}