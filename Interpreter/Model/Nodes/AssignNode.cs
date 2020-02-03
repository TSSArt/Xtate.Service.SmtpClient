using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class AssignNode : ExecutableEntityNode, IAssign, IAncestorProvider, IDebugEntityId
	{
		private readonly Assign _entity;

		public AssignNode(LinkedListNode<int> documentIdNode, in Assign entity) : base(documentIdNode, (IAssign) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		public ILocationExpression Location => _entity.Location;

		public IValueExpression Expression => _entity.Expression;

		public string InlineContent => _entity.InlineContent;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.AssignNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Location, Location);
			bucket.AddEntity(Key.Expression, Expression);
			bucket.Add(Key.InlineContent, InlineContent);
		}
	}
}