namespace TSSArt.StateMachine
{
	internal class AncestorContainer : IAncestorProvider
	{
		private readonly object? _ancestor;

		public AncestorContainer(object value, object? ancestor)
		{
			Value = value;
			_ancestor = ancestor;
		}

		public object Value { get; }

		object? IAncestorProvider.Ancestor => _ancestor;
	}
}