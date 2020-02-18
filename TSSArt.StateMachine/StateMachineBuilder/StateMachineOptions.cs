namespace TSSArt.StateMachine
{
	public struct StateMachineOptions : IStateMachineOptions
	{
		public PersistenceLevel? PersistenceLevel           { get; set; }
		public bool?             SynchronousEventProcessing { get; set; }
		public int?              ExternalQueueSize          { get; set; }
	}
}