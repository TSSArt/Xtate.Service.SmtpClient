using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IDataModel : IEntity
	{
		IReadOnlyList<IData> Data { get; }
	}
}