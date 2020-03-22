﻿using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct ForEachEntity : IForEach, IVisitorEntity<ForEachEntity, IForEach>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action { get; set; }
		public IValueExpression?                 Array  { get; set; }
		public ILocationExpression?              Index  { get; set; }
		public ILocationExpression?              Item   { get; set; }

		void IVisitorEntity<ForEachEntity, IForEach>.Init(IForEach source)
		{
			Ancestor = source;
			Action = source.Action;
			Array = source.Array;
			Index = source.Index;
			Item = source.Item;
		}

		bool IVisitorEntity<ForEachEntity, IForEach>.RefEquals(in ForEachEntity other) =>
				Action == other.Action &&
				ReferenceEquals(Array, other.Array) &&
				ReferenceEquals(Index, other.Index) &&
				ReferenceEquals(Item, other.Item);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}