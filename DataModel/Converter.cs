using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public static class Converter
	{
		public static async Task<DataModelValue> GetData(string content, IObjectEvaluator contentExpressionEvaluator, IReadOnlyList<ILocationEvaluator> nameEvaluatorList,
												 IReadOnlyList<DefaultParam> parameterList, IExecutionContext executionContext, CancellationToken token)
		{
			var attrCount = (nameEvaluatorList?.Count ?? 0) + (parameterList?.Count ?? 0);

			if (attrCount == 0)
			{
				if (contentExpressionEvaluator == null)
				{
					return new DataModelValue(content);
				}

				var obj = await contentExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);

				return DataModelValue.FromObject(obj.ToObject());
			}

			var attributes = new DataModelObject();

			if (nameEvaluatorList != null)
			{
				foreach (var locationEvaluator in nameEvaluatorList)
				{
					var name = locationEvaluator.GetName(executionContext);
					var value = locationEvaluator.GetValue(executionContext).ToObject();

					attributes[name] = DataModelValue.FromObject(value);
				}
			}

			if (parameterList != null)
			{
				foreach (var param in parameterList)
				{
					var name = param.Name;
					object value = null;

					if (param.ExpressionEvaluator != null)
					{
						value = (await param.ExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false)).ToObject();
					}
					else if (param.LocationEvaluator != null)
					{
						value = param.LocationEvaluator.GetValue(executionContext).ToObject();
					}

					attributes[name] = DataModelValue.FromObject(value);
				}
			}

			return new DataModelValue(attributes);
		}
	}
}