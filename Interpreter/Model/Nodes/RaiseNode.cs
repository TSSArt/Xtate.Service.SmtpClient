using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class RaiseNode : ExecutableEntityNode, IRaise, IAncestorProvider, IDebugEntityId
	{
		private readonly Raise _entity;

		public RaiseNode(LinkedListNode<int> documentIdNode, in Raise entity) : base(documentIdNode, (IRaise) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public IOutgoingEvent Event => _entity.Event;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.RaiseNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Event, Event);
		}
	}
}