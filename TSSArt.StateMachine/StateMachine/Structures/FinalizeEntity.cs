using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct FinalizeEntity : IFinalize, IVisitorEntity<FinalizeEntity, IFinalize>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action { get; set; }

		void IVisitorEntity<FinalizeEntity, IFinalize>.Init(IFinalize source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IVisitorEntity<FinalizeEntity, IFinalize>.RefEquals(in FinalizeEntity other) => Action == other.Action;

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}