using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IOnEntry : IEntity
	{
		ImmutableArray<IExecutableEntity> Action { get; }
	}
}