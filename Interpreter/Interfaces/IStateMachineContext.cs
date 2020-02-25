using System;

namespace TSSArt.StateMachine
{
	internal interface IStateMachineContext : IAsyncDisposable
	{
		IPersistenceContext         PersistenceContext     { get; }
		IExecutionContext           ExecutionContext       { get; }
		EntityQueue<IEvent>         InternalQueue          { get; }
		DataModelObject             DataModel              { get; }
		DataModelObject             ConfigurationObject    { get; }
		DataModelObject             InterpreterObject      { get; }
		DataModelObject             DataModelHandlerObject { get; }
		OrderedSet<StateEntityNode> Configuration          { get; }
		OrderedSet<StateEntityNode> StatesToInvoke         { get; }
		KeyList<StateEntityNode>    HistoryValue           { get; }
	}
}