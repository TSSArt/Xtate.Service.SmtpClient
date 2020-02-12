namespace TSSArt.StateMachine
{
	public struct ContentBody : IContentBody, IVisitorEntity<ContentBody, IContentBody>, IAncestorProvider
	{
		public string Value { get; set; }

		void IVisitorEntity<ContentBody, IContentBody>.Init(IContentBody source)
		{
			Ancestor = source;
			Value = source.Value;
		}

		bool IVisitorEntity<ContentBody, IContentBody>.RefEquals(in ContentBody other) => ReferenceEquals(Value, other.Value);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}