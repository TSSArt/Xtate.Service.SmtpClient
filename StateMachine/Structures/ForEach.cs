using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct ForEach : IForEach, IEntity<ForEach, IForEach>, IAncestorProvider
	{
		public IReadOnlyList<IExecutableEntity> Action { get; set; }
		public IValueExpression                 Array  { get; set; }
		public ILocationExpression              Index  { get; set; }
		public ILocationExpression              Item   { get; set; }

		void IEntity<ForEach, IForEach>.Init(IForEach source)
		{
			Ancestor = source;
			Action = source.Action;
			Array = source.Array;
			Index = source.Index;
			Item = source.Item;
		}

		bool IEntity<ForEach, IForEach>.RefEquals(in ForEach other) =>
				ReferenceEquals(Action, other.Action) &&
				ReferenceEquals(Array, other.Array) &&
				ReferenceEquals(Index, other.Index) &&
				ReferenceEquals(Item, other.Item);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}