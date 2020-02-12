namespace TSSArt.StateMachine
{
	public struct CustomAction : ICustomAction, IVisitorEntity<CustomAction, ICustomAction>, IAncestorProvider
	{
		public string Xml { get; set; }

		void IVisitorEntity<CustomAction, ICustomAction>.Init(ICustomAction source)
		{
			Ancestor = source;
			Xml = source.Xml;
		}

		bool IVisitorEntity<CustomAction, ICustomAction>.RefEquals(in CustomAction other) => ReferenceEquals(Xml, other.Xml);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}