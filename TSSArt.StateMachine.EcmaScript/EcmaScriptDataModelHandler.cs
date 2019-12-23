using System;
using System.Collections.Generic;
using Jint.Parser;
using Jint.Parser.Ast;

namespace TSSArt.StateMachine.EcmaScript
{
	public class EcmaScriptDataModelHandler : DataModelHandlerBase
	{
		private const string DataModelType      = "http://tssart.com/scxml/datamodel/#ECMAScript";
		private const string DataModelTypeAlias = "ecmascript";

		public static readonly IDataModelHandlerFactory Factory = new DataModelHandlerFactory();

		private static readonly ParserOptions    ParserOptions = new ParserOptions { Tolerant = true };
		private readonly        JavaScriptParser _parser       = new JavaScriptParser();

		private EcmaScriptDataModelHandler(StateMachineVisitor masterVisitor) : base(masterVisitor) { }

		public override void ExecutionContextCreated(IExecutionContext executionContext, IDictionary<string, string> dataModelVars)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));
			if (dataModelVars == null) throw new ArgumentNullException(nameof(dataModelVars));

			base.ExecutionContextCreated(executionContext, dataModelVars);

			executionContext.RuntimeItems[EcmaScriptEngine.Key] = new EcmaScriptEngine(executionContext);

			dataModelVars[EcmaScriptHelper.JintVersionPropertyName] = EcmaScriptHelper.JintVersionValue;
		}

		private Program Parse(string source) => _parser.Parse(source, ParserOptions);

		private string GetErrorMessage(ParserException ex) => $"{ex.Message} ({ex.Description}). Line: {ex.LineNumber}. Column: {ex.Column}.";

		protected override void Build(ref IForEach forEach, ref ForEach forEachProperties)
		{
			base.Build(ref forEach, ref forEachProperties);

			if (ValidationOnly)
			{
				return;
			}

			forEach = new EcmaScriptForEachEvaluator(forEachProperties);
		}

		protected override void Build(ref IValueExpression valueExpression, ref ValueExpression valueExpressionProperties)
		{
			var program = Parse(valueExpressionProperties.Expression);

			foreach (var parserException in program.Errors)
			{
				AddErrorMessage(GetErrorMessage(parserException));
			}

			if (!ValidationOnly)
			{
				valueExpression = new EcmaScriptValueExpressionEvaluator(valueExpressionProperties, program);
			}

			base.Build(ref valueExpression, ref valueExpressionProperties);
		}

		protected override void Build(ref IConditionExpression conditionExpression, ref ConditionExpression conditionExpressionProperties)
		{
			var program = Parse(conditionExpressionProperties.Expression);

			foreach (var parserException in program.Errors)
			{
				AddErrorMessage(GetErrorMessage(parserException));
			}

			if (!ValidationOnly)
			{
				conditionExpression = new EcmaScriptConditionExpressionEvaluator(conditionExpressionProperties, program);
			}

			base.Build(ref conditionExpression, ref conditionExpressionProperties);
		}

		protected override void Build(ref ILocationExpression locationExpression, ref LocationExpression locationExpressionProperties)
		{
			var program = Parse(locationExpressionProperties.Expression);

			foreach (var parserException in program.Errors)
			{
				AddErrorMessage(GetErrorMessage(parserException));
			}

			var leftExpression = EcmaScriptLocationExpressionEvaluator.GetLeftExpression(program);

			if (leftExpression == null)
			{
				AddErrorMessage(Resources.Error_InvalidLocationExpression);
			}

			if (!ValidationOnly)
			{
				locationExpression = new EcmaScriptLocationExpressionEvaluator(locationExpressionProperties, program, leftExpression);
			}

			base.Build(ref locationExpression, ref locationExpressionProperties);
		}

		protected override void Build(ref IScriptExpression scriptExpression, ref ScriptExpression scriptExpressionProperties)
		{
			var program = Parse(scriptExpressionProperties.Expression);

			foreach (var parserException in program.Errors)
			{
				AddErrorMessage(GetErrorMessage(parserException));
			}

			if (!ValidationOnly)
			{
				scriptExpression = new EcmaScriptScriptExpressionEvaluator(scriptExpressionProperties, program);
			}

			base.Build(ref scriptExpression, ref scriptExpressionProperties);
		}

		protected override void Build(ref IExternalScriptExpression externalScriptExpression, ref ExternalScriptExpression externalScriptExpressionProperties)
		{
			if (!ValidationOnly)
			{
				externalScriptExpression = new EcmaScriptExternalScriptExpressionEvaluator(externalScriptExpressionProperties);
			}

			base.Build(ref externalScriptExpression, ref externalScriptExpressionProperties);
		}

		protected override void Build(ref IContentBody contentBody, ref ContentBody contentBodyProperties)
		{
			if (!ValidationOnly)
			{
				contentBody = new EcmaScriptContentBodyEvaluator(contentBodyProperties);
			}

			base.Build(ref contentBody, ref contentBodyProperties);
		}

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
			public bool CanHandle(string dataModelType) => dataModelType == DataModelType || dataModelType == DataModelTypeAlias;

			public IDataModelHandler CreateHandler(StateMachineVisitor masterVisitor) => new EcmaScriptDataModelHandler(masterVisitor);
		}
	}
}