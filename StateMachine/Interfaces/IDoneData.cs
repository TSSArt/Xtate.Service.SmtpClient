using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IDoneData : IEntity
	{
		IContent              Content    { get; }
		IReadOnlyList<IParam> Parameters { get; }
	}
}