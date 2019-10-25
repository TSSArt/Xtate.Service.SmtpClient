namespace TSSArt.StateMachine
{
	public class StateMachineResult
	{
		public StateMachineResult(StateMachineExitStatus status, DataModelValue result)
		{
			Result = result;
			Status = status;
		}

		public StateMachineExitStatus Status { get; }

		public DataModelValue Result;
	}
}