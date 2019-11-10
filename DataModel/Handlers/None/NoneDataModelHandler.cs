using System;

namespace TSSArt.StateMachine
{
	public class NoneDataModelHandler : DataModelHandlerBase
	{
		public const string DataModelType = "none";

		public static readonly IDataModelHandlerFactory Factory = new DataModelHandlerFactory();

		private NoneDataModelHandler() { }

		private NoneDataModelHandler(StateMachineVisitor masterVisitor) : base(masterVisitor) { }

		protected override void Visit(ref IForEach forEach) => AddErrorMessage(message: "ForEach not supported in NONE data model.");

		protected override void Visit(ref IScript script) => AddErrorMessage(message: "Scripting not supported in NONE data model.");

		protected override void Visit(ref IDataModel dataModel) => AddErrorMessage(message: "DataModel not supported in NONE data model.");

		protected override void Visit(ref IDoneData doneData) => AddErrorMessage(message: "DoneData not supported in NONE data model.");

		protected override void Visit(ref IValueExpression expression) => AddErrorMessage(message: "'Value expression' not supported in NONE data model.");

		protected override void Visit(ref ILocationExpression expression) => AddErrorMessage(message: "'Location expression' not supported in NONE data model.");

		public static void Validate(IStateMachine stateMachine)
		{
			if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));

			if (stateMachine.DataModelType == null || stateMachine.DataModelType == DataModelType)
			{
				var validator = new NoneDataModelHandler();
				validator.SetRootPath(stateMachine);
				validator.Visit(ref stateMachine);
				validator.ThrowIfErrors();
			}
		}

		protected override void Build(ref IConditionExpression conditionExpression, ref ConditionExpression properties)
		{
			base.Build(ref conditionExpression, ref properties);

			var expression = properties.Expression;

			if (!expression.StartsWith(value: "In(", StringComparison.Ordinal) || !expression.EndsWith(value: ")", StringComparison.Ordinal))
			{
				AddErrorMessage(message: "Incorrect format of condition expression for NONE data model");
			}

			try
			{
				var inState = (Identifier) expression.Substring(startIndex: 3, expression.Length - 4).Trim();

				if (!ValidationOnly)
				{
					conditionExpression = new NoneConditionExpressionEvaluator(properties, inState);
				}
			}
			catch (ArgumentException ex)
			{
				AddErrorMessage(ex.Message);
			}
		}

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
			public bool CanHandle(string dataModelType) => dataModelType == DataModelType;

			public IDataModelHandler CreateHandler(StateMachineVisitor masterVisitor) => new NoneDataModelHandler(masterVisitor);
		}
	}
}