using System.Collections.Immutable;

namespace Xtate
{
	public struct CustomAction : ICustomAction, IVisitorEntity<CustomAction, ICustomAction>, IAncestorProvider
	{
		internal object? Ancestor;

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => Ancestor;

	#endregion

	#region Interface ICustomAction

		public string? Xml { get; set; }

		public ImmutableArray<ILocationExpression> Locations { get; set; }

		public ImmutableArray<IValueExpression> Values { get; set; }

	#endregion

	#region Interface IVisitorEntity<CustomAction,ICustomAction>

		void IVisitorEntity<CustomAction, ICustomAction>.Init(ICustomAction source)
		{
			Ancestor = source;
			Xml = source.Xml;
			Locations = source.Locations;
			Values = source.Values;
		}

		bool IVisitorEntity<CustomAction, ICustomAction>.RefEquals(ref CustomAction other) =>
				ReferenceEquals(Xml, other.Xml) &&
				Locations == other.Locations &&
				Values == other.Values;

	#endregion
	}
}