using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct OnEntry : IOnEntry, IVisitorEntity<OnEntry, IOnEntry>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action { get; set; }

		void IVisitorEntity<OnEntry, IOnEntry>.Init(IOnEntry source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IVisitorEntity<OnEntry, IOnEntry>.RefEquals(in OnEntry other) => Action == other.Action;

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}