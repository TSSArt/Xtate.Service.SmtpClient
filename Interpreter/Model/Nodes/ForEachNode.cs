using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class ForEachNode : ExecutableEntityNode, IForEach, IAncestorProvider, IDebugEntityId
	{
		private readonly ForEach _entity;

		public ForEachNode(LinkedListNode<int> documentIdNode, in ForEach entity) : base(documentIdNode, (IForEach) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public IValueExpression Array => _entity.Array;

		public ILocationExpression Item => _entity.Item;

		public ILocationExpression Index => _entity.Index;

		public ImmutableArray<IExecutableEntity> Action => _entity.Action;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ForEachNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Array, _entity.Array);
			bucket.AddEntity(Key.Item, _entity.Item);
			bucket.AddEntity(Key.Index, _entity.Index);
			bucket.AddEntityList(Key.Action, Action);
		}
	}
}