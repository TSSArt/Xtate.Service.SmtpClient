using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IDataModel : IEntity
	{
		ImmutableArray<IData> Data { get; }
	}
}