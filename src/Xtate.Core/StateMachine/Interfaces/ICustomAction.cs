using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface ICustomAction : IExecutableEntity
	{
		string? Xml { get; }

		ImmutableArray<ILocationExpression> Locations { get; }

		ImmutableArray<IValueExpression> Values { get; }
	}
}