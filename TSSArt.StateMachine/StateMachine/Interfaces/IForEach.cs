using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IForEach : IExecutableEntity
	{
		IValueExpression                 Array  { get; }
		ILocationExpression              Item   { get; }
		ILocationExpression              Index  { get; }
		ImmutableArray<IExecutableEntity> Action { get; }
	}
}