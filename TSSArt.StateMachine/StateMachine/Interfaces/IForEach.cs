using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IForEach : IExecutableEntity
	{
		IValueExpression                 Array  { get; }
		ILocationExpression              Item   { get; }
		ILocationExpression              Index  { get; }
		IReadOnlyList<IExecutableEntity> Action { get; }
	}
}