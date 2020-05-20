namespace TSSArt.StateMachine
{
	public enum StateMachineInterpreterState
	{
		Accepted,
		Started,
		Exited,
		Waiting,
		Resumed,
		Halted,
		Destroying,
		Suspended,
		QueueClosed
	}
}