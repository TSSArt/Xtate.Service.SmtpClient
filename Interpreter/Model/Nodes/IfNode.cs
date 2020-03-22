using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class IfNode : ExecutableEntityNode, IIf, IAncestorProvider, IDebugEntityId
	{
		private readonly IfEntity _entity;

		public IfNode(LinkedListNode<int> documentIdNode, in IfEntity entity) : base(documentIdNode, (IIf?) entity.Ancestor) => _entity = entity;

		object? IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => @$"(#{DocumentId})";

		public IConditionExpression? Condition => _entity.Condition;

		public ImmutableArray<IExecutableEntity> Action => _entity.Action;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.IfNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Condition, Condition);
			bucket.AddEntityList(Key.Action, Action);
		}
	}
}