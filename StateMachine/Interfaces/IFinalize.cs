using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public interface IFinalize : IEntity
	{
		/**/ImmutableArray<IExecutableEntity> Action { get; }
	}
}