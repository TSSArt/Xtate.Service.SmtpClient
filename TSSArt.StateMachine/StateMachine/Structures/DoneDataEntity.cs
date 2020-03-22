using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct DoneDataEntity : IDoneData, IVisitorEntity<DoneDataEntity, IDoneData>, IAncestorProvider
	{
		public IContent?              Content    { get; set; }
		public ImmutableArray<IParam> Parameters { get; set; }

		void IVisitorEntity<DoneDataEntity, IDoneData>.Init(IDoneData source)
		{
			Ancestor = source;
			Content = source.Content;
			Parameters = source.Parameters;
		}

		bool IVisitorEntity<DoneDataEntity, IDoneData>.RefEquals(in DoneDataEntity other) =>
				ReferenceEquals(Content, other.Content) &&
				Parameters == other.Parameters;

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}