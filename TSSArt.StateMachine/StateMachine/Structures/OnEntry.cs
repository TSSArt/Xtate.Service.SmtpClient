using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct OnEntry : IOnEntry, IEntity<OnEntry, IOnEntry>, IAncestorProvider
	{
		public IReadOnlyList<IExecutableEntity> Action;

		IReadOnlyList<IExecutableEntity> IOnEntry.Action => Action;

		void IEntity<OnEntry, IOnEntry>.Init(IOnEntry source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IEntity<OnEntry, IOnEntry>.RefEquals(in OnEntry other) => ReferenceEquals(Action, other.Action);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}