namespace TSSArt.StateMachine
{
	public struct CustomAction : ICustomAction, IEntity<CustomAction, ICustomAction>, IAncestorProvider
	{
		public string Xml { get; set; }

		void IEntity<CustomAction, ICustomAction>.Init(ICustomAction source)
		{
			Ancestor = source;
			Xml = source.Xml;
		}

		bool IEntity<CustomAction, ICustomAction>.RefEquals(in CustomAction other) => ReferenceEquals(Xml, other.Xml);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}