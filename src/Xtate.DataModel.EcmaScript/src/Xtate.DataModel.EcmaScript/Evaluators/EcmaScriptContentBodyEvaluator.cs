using System;
using System.Text.Json;

namespace Xtate.DataModel.EcmaScript
{
	internal class EcmaScriptContentBodyEvaluator : DefaultContentBodyEvaluator
	{
		public EcmaScriptContentBodyEvaluator(in ContentBody contentBody) : base(contentBody) { }

		protected override DataModelValue ParseToDataModel(ref Exception? parseException)
		{
			try
			{
				return DataModelConverter.FromJson(Value);
			}
			catch (JsonException ex)
			{
				parseException = ex;

				return Value.NormalizeSpaces();
			}
		}
	}
}