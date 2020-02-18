namespace TSSArt.StateMachine
{
	public interface IStateMachineOptionsBuilder
	{
		void SetPersistenceLevel(PersistenceLevel persistenceLevel);
		void SetSynchronousEventProcessing(bool value);
		void SetExternalQueueSize(int size);
	}
}