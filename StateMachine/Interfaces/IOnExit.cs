using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IOnExit : IEntity
	{
		IReadOnlyList<IExecutableEntity> Action { get; }
	}
}