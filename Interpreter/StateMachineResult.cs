namespace TSSArt.StateMachine
{
	public class StateMachineResult
	{
		public DataModelValue Result;

		public StateMachineResult(StateMachineExitStatus status, DataModelValue result)
		{
			Result = result;
			Status = status;
		}

		public StateMachineExitStatus Status { get; }
	}
}