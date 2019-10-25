using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct DoneData : IDoneData, IEntity<DoneData, IDoneData>, IAncestorProvider
	{
		public IContent              Content;
		public IReadOnlyList<IParam> Parameters;

		IContent IDoneData.Content => Content;

		IReadOnlyList<IParam> IDoneData.Parameters => Parameters;

		void IEntity<DoneData, IDoneData>.Init(IDoneData source)
		{
			Ancestor = source;
			Content = source.Content;
			Parameters = source.Parameters;
		}

		bool IEntity<DoneData, IDoneData>.RefEquals(in DoneData other) =>
				ReferenceEquals(Content, other.Content) &&
				ReferenceEquals(Parameters, other.Parameters);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}