using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IFinal : IStateEntity
	{
		IIdentifier             Id       { get; }
		IReadOnlyList<IOnEntry> OnEntry  { get; }
		IReadOnlyList<IOnExit>  OnExit   { get; }
		IDoneData               DoneData { get; }
	}
}