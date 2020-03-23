namespace TSSArt.StateMachine
{
	public struct StateMachineOptions : IStateMachineOptions
	{
	#region Interface IStateMachineOptions

		public PersistenceLevel? PersistenceLevel           { get; set; }
		public bool?             SynchronousEventProcessing { get; set; }
		public int?              ExternalQueueSize          { get; set; }

	#endregion
	}
}