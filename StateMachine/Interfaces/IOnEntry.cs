using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IOnEntry : IEntity
	{
		IReadOnlyList<IExecutableEntity> Action { get; }
	}
}