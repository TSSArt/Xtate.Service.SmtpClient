using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IDoneData : IEntity
	{
		IContent               Content    { get; }
		ImmutableArray<IParam> Parameters { get; }
	}
}