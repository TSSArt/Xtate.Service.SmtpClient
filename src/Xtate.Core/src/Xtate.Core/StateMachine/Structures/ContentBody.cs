namespace TSSArt.StateMachine
{
	public struct ContentBody : IContentBody, IVisitorEntity<ContentBody, IContentBody>, IAncestorProvider
	{
		internal object? Ancestor;

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => Ancestor;

	#endregion

	#region Interface IContentBody

		public string? Value { get; set; }

	#endregion

	#region Interface IVisitorEntity<ContentBody,IContentBody>

		void IVisitorEntity<ContentBody, IContentBody>.Init(IContentBody source)
		{
			Ancestor = source;
			Value = source.Value;
		}

		bool IVisitorEntity<ContentBody, IContentBody>.RefEquals(in ContentBody other) => ReferenceEquals(Value, other.Value);

	#endregion
	}
}