namespace TSSArt.StateMachine
{
	public class BuilderFactory : IBuilderFactory
	{
		public IStateMachineBuilder CreateStateMachineBuilder() => new StateMachineBuilder();

		public IStateBuilder CreateStateBuilder() => new StateBuilder();

		public IParallelBuilder CreateParallelBuilder() => new ParallelBuilder();

		public IHistoryBuilder CreateHistoryBuilder() => new HistoryBuilder();

		public IInitialBuilder CreateInitialBuilder() => new InitialBuilder();

		public IFinalBuilder CreateFinalBuilder() => new FinalBuilder();

		public ITransitionBuilder CreateTransitionBuilder() => new TransitionBuilder();

		public ILogBuilder CreateLogBuilder() => new LogBuilder();

		public ISendBuilder CreateSendBuilder() => new SendBuilder();

		public IParamBuilder CreateParamBuilder() => new ParamBuilder();

		public IContentBuilder CreateContentBuilder() => new ContentBuilder();

		public IOnEntryBuilder CreateOnEntryBuilder() => new OnEntryBuilder();

		public IOnExitBuilder CreateOnExitBuilder() => new OnExitBuilder();

		public IInvokeBuilder CreateInvokeBuilder() => new InvokeBuilder();

		public IFinalizeBuilder CreateFinalizeBuilder() => new FinalizeBuilder();

		public IScriptBuilder CreateScriptBuilder() => new ScriptBuilder();

		public IDataModelBuilder CreateDataModelBuilder() => new DataModelBuilder();

		public IDataBuilder CreateDataBuilder() => new DataBuilder();

		public IDoneDataBuilder CreateDoneDataBuilder() => new DoneDataBuilder();

		public IAssignBuilder CreateAssignBuilder() => new AssignBuilder();

		public IRaiseBuilder CreateRaiseBuilder() => new RaiseBuilder();

		public ICancelBuilder CreateCancelBuilder() => new CancelBuilder();

		public IForEachBuilder CreateForeachBuilder() => new ForEachBuilder();

		public IIfBuilder CreateIfBuilder() => new IfBuilder();

		public IElseBuilder CreateElseBuilder() => new ElseBuilder();

		public IElseIfBuilder CreateElseIfBuilder() => new ElseIfBuilder();
	}
}