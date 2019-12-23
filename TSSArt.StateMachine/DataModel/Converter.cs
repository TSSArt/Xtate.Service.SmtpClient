using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public static class Converter
	{
		public static ValueTask<DataModelValue> GetData(IObjectEvaluator contentBodyEvaluator, IObjectEvaluator contentExpressionEvaluator, IReadOnlyList<ILocationEvaluator> nameEvaluatorList,
														IReadOnlyList<DefaultParam> parameterList, IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			var attrCount = (nameEvaluatorList?.Count ?? 0) + (parameterList?.Count ?? 0);

			if (attrCount == 0)
			{
				return GetContent(contentBodyEvaluator, contentExpressionEvaluator, executionContext, token);
			}

			return GetParameters(nameEvaluatorList, parameterList, executionContext, token);
		}

		public static async ValueTask<DataModelValue> GetContent(IObjectEvaluator contentBodyEvaluator, IObjectEvaluator contentExpressionEvaluator, IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			if (contentExpressionEvaluator != null)
			{
				var obj = await contentExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);

				return DataModelValue.FromObject(obj.ToObject()).DeepClone(true);
			}

			if (contentBodyEvaluator != null)
			{
				var obj = await contentBodyEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);

				return DataModelValue.FromObject(obj.ToObject()).DeepClone(true);
			}

			return DataModelValue.Undefined;
		}

		public static async ValueTask<DataModelValue> GetParameters(IReadOnlyList<ILocationEvaluator> nameEvaluatorList, IReadOnlyList<DefaultParam> parameterList,
																	IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			var attrCount = (nameEvaluatorList?.Count ?? 0) + (parameterList?.Count ?? 0);

			if (attrCount == 0)
			{
				return DataModelValue.Undefined;
			}

			var attributes = new DataModelObject();

			if (nameEvaluatorList != null)
			{
				foreach (var locationEvaluator in nameEvaluatorList)
				{
					var name = locationEvaluator.GetName(executionContext);
					var value = locationEvaluator.GetValue(executionContext).ToObject();

					attributes[name] = DataModelValue.FromObject(value).DeepClone(true);
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

					attributes[name] = DataModelValue.FromObject(value).DeepClone(true);
				}
			}

			attributes.Freeze();

			return new DataModelValue(attributes);
		}
	}
}