using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IOnExit : IEntity
	{
		ImmutableArray<IExecutableEntity> Action { get; }
	}
}