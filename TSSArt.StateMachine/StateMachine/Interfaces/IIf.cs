using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public interface IIf : IExecutableEntity
	{
		IConditionExpression             Condition { get; }
		/**/ImmutableArray<IExecutableEntity> Action    { get; }
	}
}