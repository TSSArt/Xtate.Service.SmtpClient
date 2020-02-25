namespace TSSArt.StateMachine
{
	public interface IStateMachineOptions
	{
		PersistenceLevel? PersistenceLevel { get; }

		bool? SynchronousEventProcessing { get; }

		int? ExternalQueueSize { get; }
	}
}