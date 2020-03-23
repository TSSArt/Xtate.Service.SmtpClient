using System;

namespace TSSArt.StateMachine
{
	internal sealed class NoneDataModelHandler : DataModelHandlerBase
	{
		public const string DataModelType = "none";

		public static readonly IDataModelHandlerFactory Factory = new DataModelHandlerFactory();

		private NoneDataModelHandler(IErrorProcessor errorProcessor) : base(errorProcessor) { }

		protected override void Visit(ref IForEach forEach) => AddErrorMessage(forEach, Resources.ErrorMesasge_ForEach_not_supported_in_NONE_data_model_);

		protected override void Visit(ref IScript script) => AddErrorMessage(script, Resources.ErrorMesasge_Scripting_not_supported_in_NONE_data_model_);

		protected override void Visit(ref IDataModel dataModel) => AddErrorMessage(dataModel, Resources.ErrorMesasge_DataModel_not_supported_in_NONE_data_model);

		protected override void Visit(ref IDoneData doneData) => AddErrorMessage(doneData, Resources.ErrorMesasge_DoneData_not_supported_in_NONE_data_model);

		protected override void Visit(ref IValueExpression expression) => AddErrorMessage(expression, Resources.ErrorMesasge_Value_expression__not_supported_in_NONE_data_model);

		protected override void Visit(ref ILocationExpression expression) => AddErrorMessage(expression, Resources.ErrorMesasge_Location_expression__not_supported_in_NONE_data_model);

		protected override void Build(ref IConditionExpression conditionExpression, ref ConditionExpression properties)
		{
			base.Build(ref conditionExpression, ref properties);

			var expression = properties.Expression!;

			if (!expression.StartsWith(value: @"In(", StringComparison.Ordinal) || !expression.EndsWith(value: @")", StringComparison.Ordinal))
			{
				AddErrorMessage(conditionExpression, Resources.ErrorMesasge_Incorrect_format_of_condition_expression_for_NONE_data_model);

				return;
			}

			try
			{
				var inState = (Identifier) expression.Substring(startIndex: 3, expression.Length - 4).Trim();

				conditionExpression = new NoneConditionExpressionEvaluator(properties, inState);
			}
			catch (ArgumentException ex)
			{
				AddErrorMessage(conditionExpression, Resources.ErrorMesasge_Incorrect_condition_expression, ex);
			}
		}

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
		#region Interface IDataModelHandlerFactory

			public bool CanHandle(string dataModelType) => dataModelType == DataModelType;

			public IDataModelHandler CreateHandler(IErrorProcessor errorProcessor) => new NoneDataModelHandler(errorProcessor);

		#endregion
		}
	}
}