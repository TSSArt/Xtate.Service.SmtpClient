using System;
using System.Collections.Immutable;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal static class DataConverter
	{
		public static ValueTask<DataModelValue> GetData(IValueEvaluator? contentBodyEvaluator, IObjectEvaluator? contentExpressionEvaluator, ImmutableArray<ILocationEvaluator> nameEvaluatorList,
														ImmutableArray<DefaultParam> parameterList, IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			if (nameEvaluatorList.IsDefaultOrEmpty && parameterList.IsDefaultOrEmpty)
			{
				return GetContent(contentBodyEvaluator, contentExpressionEvaluator, executionContext, token);
			}

			return GetParameters(nameEvaluatorList, parameterList, executionContext, token);
		}

		public static async ValueTask<DataModelValue> GetContent(IValueEvaluator? contentBodyEvaluator, IObjectEvaluator? contentExpressionEvaluator,
																 IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			if (contentExpressionEvaluator != null)
			{
				var obj = await contentExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);

				return DataModelValue.FromObject(obj.ToObject()).AsConstant();
			}

			if (contentBodyEvaluator is IObjectEvaluator objectEvaluator)
			{
				var obj = await objectEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);

				return DataModelValue.FromObject(obj.ToObject()).AsConstant();
			}

			if (contentBodyEvaluator is IStringEvaluator stringEvaluator)
			{
				var str = await stringEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false);

				return new DataModelValue(str);
			}

			return default;
		}

		public static async ValueTask<DataModelValue> GetParameters(ImmutableArray<ILocationEvaluator> nameEvaluatorList, ImmutableArray<DefaultParam> parameterList,
																	IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			var length = (nameEvaluatorList.IsDefault ? 0 : nameEvaluatorList.Length) + (parameterList.IsDefault ? 0 : parameterList.Length);

			if (length == 0)
			{
				return default;
			}

			var attributes = new DataModelObject(length);

			if (!nameEvaluatorList.IsDefaultOrEmpty)
			{
				foreach (var locationEvaluator in nameEvaluatorList)
				{
					var name = locationEvaluator.GetName(executionContext);
					var value = await locationEvaluator.GetValue(executionContext, token).ConfigureAwait(false);

					attributes[name] = DataModelValue.FromObject(value).AsConstant();
				}
			}

			if (!parameterList.IsDefaultOrEmpty)
			{
				foreach (var param in parameterList)
				{
					var name = param.Name;
					object? value = null;

					if (param.ExpressionEvaluator != null)
					{
						value = (await param.ExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false)).ToObject();
					}
					else if (param.LocationEvaluator != null)
					{
						value = await param.LocationEvaluator.GetValue(executionContext, token).ConfigureAwait(false);
					}

					attributes[name] = DataModelValue.FromObject(value).AsConstant();
				}
			}

			return new DataModelValue(attributes);
		}

		public static DataModelValue FromContent(string content, ContentType? contentType)
		{
			var _ = contentType;

			return new DataModelValue(content);
		}

		public static DataModelValue FromEvent(IEvent evt)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			var eventObject = new DataModelObject(capacity: 7)
							  {
									  [@"name"] = new DataModelValue(EventName.ToName(evt.NameParts)),
									  [@"type"] = new DataModelValue(GetTypeString(evt.Type)),
									  [@"sendid"] = new DataModelValue(evt.SendId),
									  [@"origin"] = new DataModelValue(evt.Origin?.ToString()),
									  [@"origintype"] = new DataModelValue(evt.OriginType?.ToString()),
									  [@"invokeid"] = new DataModelValue(evt.InvokeId),
									  [@"data"] = evt.Data.AsConstant()
							  };

			eventObject.MakeDeepConstant();

			return new DataModelValue(eventObject);

			static string GetTypeString(EventType eventType)
			{
				return eventType switch
				{
						EventType.Platform => @"platform",
						EventType.Internal => @"internal",
						EventType.External => @"external",
						_ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, message: null)
				};
			}
		}

		public static DataModelValue FromException(Exception exception)
		{
			if (exception == null) throw new ArgumentNullException(nameof(exception));

			var exceptionData = new DataModelObject(capacity: 6)
								{
										[@"message"] = new DataModelValue(exception.Message),
										[@"typeName"] = new DataModelValue(exception.GetType().Name),
										[@"source"] = new DataModelValue(exception.Source),
										[@"typeFullName"] = new DataModelValue(exception.GetType().FullName),
										[@"stackTrace"] = new DataModelValue(exception.StackTrace),
										[@"text"] = new DataModelValue(exception.ToString())
								};

			exceptionData.MakeDeepConstant();

			return new DataModelValue(exceptionData);
		}
	}
}