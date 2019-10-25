using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IIf : IExecutableEntity
	{
		IConditionExpression             Condition { get; }
		IReadOnlyList<IExecutableEntity> Action    { get; }
	}
}