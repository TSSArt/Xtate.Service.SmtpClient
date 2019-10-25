using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IFinalize : IEntity
	{
		IReadOnlyList<IExecutableEntity> Action { get; }
	}
}