using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class RaiseNode : ExecutableEntityNode, IRaise, IAncestorProvider, IDebugEntityId
	{
		private readonly RaiseEntity _entity;

		public RaiseNode(LinkedListNode<int> documentIdNode, in RaiseEntity entity) : base(documentIdNode, (IRaise?) entity.Ancestor) => _entity = entity;

		object? IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => @$"(#{DocumentId})";

		public IOutgoingEvent? OutgoingEvent => _entity.OutgoingEvent;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.RaiseNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Event, OutgoingEvent);
		}
	}
}