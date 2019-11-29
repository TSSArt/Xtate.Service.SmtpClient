namespace TSSArt.StateMachine
{
	public interface IBuilderFactory
	{
		IStateMachineBuilder CreateStateMachineBuilder();
		IStateBuilder        CreateStateBuilder();
		IParallelBuilder     CreateParallelBuilder();
		IHistoryBuilder      CreateHistoryBuilder();
		IInitialBuilder      CreateInitialBuilder();
		IFinalBuilder        CreateFinalBuilder();
		ITransitionBuilder   CreateTransitionBuilder();
		ILogBuilder          CreateLogBuilder();
		ISendBuilder         CreateSendBuilder();
		IParamBuilder        CreateParamBuilder();
		IContentBuilder      CreateContentBuilder();
		IOnEntryBuilder      CreateOnEntryBuilder();
		IOnExitBuilder       CreateOnExitBuilder();
		IInvokeBuilder       CreateInvokeBuilder();
		IFinalizeBuilder     CreateFinalizeBuilder();
		IScriptBuilder       CreateScriptBuilder();
		IDataModelBuilder    CreateDataModelBuilder();
		IDataBuilder         CreateDataBuilder();
		IDoneDataBuilder     CreateDoneDataBuilder();
		IForEachBuilder      CreateForeachBuilder();
		IIfBuilder           CreateIfBuilder();
		IElseBuilder         CreateElseBuilder();
		IElseIfBuilder       CreateElseIfBuilder();
		IRaiseBuilder        CreateRaiseBuilder();
		IAssignBuilder       CreateAssignBuilder();
		ICancelBuilder       CreateCancelBuilder();
		ICustomActionBuilder CreateCustomActionBuilder();
	}
}