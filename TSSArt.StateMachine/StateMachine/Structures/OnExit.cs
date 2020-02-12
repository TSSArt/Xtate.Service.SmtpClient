using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct OnExit : IOnExit, IVisitorEntity<OnExit, IOnExit>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action { get; set; }

		void IVisitorEntity<OnExit, IOnExit>.Init(IOnExit source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IVisitorEntity<OnExit, IOnExit>.RefEquals(in OnExit other) => Action == other.Action;

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}