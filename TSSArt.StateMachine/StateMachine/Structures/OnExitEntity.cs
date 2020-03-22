using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct OnExitEntity : IOnExit, IVisitorEntity<OnExitEntity, IOnExit>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action { get; set; }

		void IVisitorEntity<OnExitEntity, IOnExit>.Init(IOnExit source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IVisitorEntity<OnExitEntity, IOnExit>.RefEquals(in OnExitEntity other) => Action == other.Action;

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}