using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class InitialNode : StateEntityNode, IInitial, IAncestorProvider, IDebugEntityId
	{
		private readonly Initial _initial;

		public InitialNode(LinkedListNode<int> documentIdNode, in Initial initial) : base(documentIdNode, children: null)
		{
			_initial = initial;
			Transition = (TransitionNode) initial.Transition;

			Transition.SetSource(this);
		}

		public InitialNode(LinkedListNode<int> documentIdNode, TransitionNode transition) : base(documentIdNode, children: null)
		{
			Transition = transition ?? throw new ArgumentNullException(nameof(transition));

			Transition.SetSource(this);
		}

		public TransitionNode Transition { get; }

		object IAncestorProvider.Ancestor => _initial.Ancestor;

		public FormattableString EntityId => $"(#{DocumentId})";

		ITransition IInitial.Transition => _initial.Transition;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.InitialNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Transition, Transition);
		}
	}
}