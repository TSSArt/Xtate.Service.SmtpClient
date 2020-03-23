namespace TSSArt.StateMachine
{
	public struct CustomAction : ICustomAction, IVisitorEntity<CustomAction, ICustomAction>, IAncestorProvider
	{
		internal object? Ancestor;

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => Ancestor;

	#endregion

	#region Interface ICustomAction

		public string? Xml { get; set; }

	#endregion

	#region Interface IVisitorEntity<CustomAction,ICustomAction>

		void IVisitorEntity<CustomAction, ICustomAction>.Init(ICustomAction source)
		{
			Ancestor = source;
			Xml = source.Xml;
		}

		bool IVisitorEntity<CustomAction, ICustomAction>.RefEquals(in CustomAction other) => ReferenceEquals(Xml, other.Xml);

	#endregion
	}
}