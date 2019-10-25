namespace TSSArt.StateMachine
{
	public enum StateMachineExitStatus
	{
		Unknown       = 0,
		Completed     = 1,
		QueueClosed   = 2,
		Suspended     = 3,
		Destroyed     = 4,
		LiveLockAbort = 5
	}
}