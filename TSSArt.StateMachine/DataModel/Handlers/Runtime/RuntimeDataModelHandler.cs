namespace TSSArt.StateMachine
{
	public class RuntimeDataModelHandler : DataModelHandlerBase
	{
		public const string DataModelType = "runtime";

		public static readonly IDataModelHandlerFactory Factory = new DataModelHandlerFactory();

		private RuntimeDataModelHandler() { }

		private RuntimeDataModelHandler(StateMachineVisitor masterVisitor) : base(masterVisitor) { }

		protected override void Visit(ref IScript script)       => AddErrorMessage(message: "Scripting not supported in RUNTIME data model.");
		protected override void Visit(ref IDataModel dataModel) => AddErrorMessage(message: "DataModel not supported in RUNTIME data model.");
		protected override void Visit(ref IDoneData doneData)   => AddErrorMessage(message: "DoneData not supported in RUNTIME data model.");

		protected override void Visit(ref IExecutableEntity executableEntity)
		{
			if (!(executableEntity is RuntimeAction) && !(executableEntity is RuntimePredicate))
			{
				AddErrorMessage(message: "RuntimeAction and RuntimePredicate objects only allowed as action and condition in RUNTIME data model.");
			}
		}

		public static void Validate(IStateMachine stateMachine)
		{
			if (stateMachine.DataModelType == DataModelType)
			{
				var validator = new RuntimeDataModelHandler();
				validator.SetRootPath(stateMachine);
				validator.Visit(ref stateMachine);
				validator.ThrowIfErrors();
			}
		}

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
			public bool CanHandle(string dataModelType) => dataModelType == DataModelType;

			public IDataModelHandler CreateHandler(StateMachineVisitor masterVisitor) => new RuntimeDataModelHandler(masterVisitor);
		}
	}
}