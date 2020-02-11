using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class CancelNode : ExecutableEntityNode, ICancel, IAncestorProvider, IDebugEntityId
	{
		private readonly Cancel _entity;

		public CancelNode(LinkedListNode<int> documentIdNode, in Cancel entity) : base(documentIdNode, (ICancel) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		public string SendId => _entity.SendId;

		public IValueExpression SendIdExpression => _entity.SendIdExpression;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.CancelNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.SendId, SendId);
			bucket.AddEntity(Key.SendIdExpression, SendIdExpression);
		}
	}
}