using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct OnExit : IOnExit, IEntity<OnExit, IOnExit>, IAncestorProvider
	{
		public IReadOnlyList<IExecutableEntity> Action { get; set; }

		void IEntity<OnExit, IOnExit>.Init(IOnExit source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IEntity<OnExit, IOnExit>.RefEquals(in OnExit other) => ReferenceEquals(Action, other.Action);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}