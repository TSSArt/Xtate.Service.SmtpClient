using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public struct DoneData : IDoneData, IEntity<DoneData, IDoneData>, IAncestorProvider
	{
		public IContent              Content    { get; set; }
		public /**/ImmutableArray<IParam> Parameters { get; set; }

		void IEntity<DoneData, IDoneData>.Init(IDoneData source)
		{
			Ancestor = source;
			Content = source.Content;
			Parameters = source.Parameters;
		}

		bool IEntity<DoneData, IDoneData>.RefEquals(in DoneData other) =>
				ReferenceEquals(Content, other.Content) &&
				Parameters == other.Parameters;

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}