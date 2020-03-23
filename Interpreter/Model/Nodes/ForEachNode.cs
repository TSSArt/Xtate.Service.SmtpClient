using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class ForEachNode : ExecutableEntityNode, IForEach, IAncestorProvider, IDebugEntityId
	{
		private readonly ForEachEntity _entity;

		public ForEachNode(LinkedListNode<int> documentIdNode, in ForEachEntity entity) : base(documentIdNode, (IForEach?) entity.Ancestor) => _entity = entity;

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _entity.Ancestor;

	#endregion

	#region Interface IDebugEntityId

		FormattableString IDebugEntityId.EntityId => @$"(#{DocumentId})";

	#endregion

	#region Interface IForEach

		public IValueExpression? Array => _entity.Array;

		public ILocationExpression? Item => _entity.Item;

		public ILocationExpression? Index => _entity.Index;

		public ImmutableArray<IExecutableEntity> Action => _entity.Action;

	#endregion

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