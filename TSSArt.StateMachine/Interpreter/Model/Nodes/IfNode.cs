using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class IfNode : ExecutableEntityNode, IIf, IAncestorProvider, IDebugEntityId
	{
		private readonly If _entity;

		public IfNode(LinkedListNode<int> documentIdNode, in If entity) : base(documentIdNode, (IIf) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public IConditionExpression Condition => _entity.Condition;

		public IReadOnlyList<IExecutableEntity> Action => _entity.Action;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.IfNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Condition, Condition);
			bucket.AddEntityList(Key.Action, Action);
		}
	}
}