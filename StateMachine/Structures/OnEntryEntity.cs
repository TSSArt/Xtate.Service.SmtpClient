using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct OnEntryEntity : IOnEntry, IVisitorEntity<OnEntryEntity, IOnEntry>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action { get; set; }

		void IVisitorEntity<OnEntryEntity, IOnEntry>.Init(IOnEntry source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IVisitorEntity<OnEntryEntity, IOnEntry>.RefEquals(in OnEntryEntity other) => Action == other.Action;

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}