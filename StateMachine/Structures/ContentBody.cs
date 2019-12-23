namespace TSSArt.StateMachine
{
	public struct ContentBody : IContentBody, IEntity<ContentBody, IContentBody>, IAncestorProvider
	{
		public string Value { get; set; }

		void IEntity<ContentBody, IContentBody>.Init(IContentBody source)
		{
			Ancestor = source;
			Value = source.Value;
		}

		bool IEntity<ContentBody, IContentBody>.RefEquals(in ContentBody other) =>  ReferenceEquals(Value, other.Value);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}