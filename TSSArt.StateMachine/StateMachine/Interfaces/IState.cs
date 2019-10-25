using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IState : IStateEntity
	{
		IIdentifier                 Id            { get; }
		IInitial                    Initial       { get; }
		IReadOnlyList<IStateEntity> States        { get; }
		IReadOnlyList<IHistory>     HistoryStates { get; }
		IReadOnlyList<ITransition>  Transitions   { get; }
		IDataModel                  DataModel     { get; }
		IReadOnlyList<IOnEntry>     OnEntry       { get; }
		IReadOnlyList<IOnExit>      OnExit        { get; }
		IReadOnlyList<IInvoke>      Invoke        { get; }
	}
}