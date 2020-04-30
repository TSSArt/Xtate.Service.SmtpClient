namespace TSSArt.StateMachine
{
	public interface IStateMachineOptions
	{
		string? Name { get; }

		PersistenceLevel? PersistenceLevel { get; }

		bool? SynchronousEventProcessing { get; }

		int? ExternalQueueSize { get; }
	}
}