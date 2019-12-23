using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptContentBodyEvaluator : DefaultContentBodyEvaluator, IObjectEvaluator
	{
		public EcmaScriptContentBodyEvaluator(in ContentBody contentBody) : base(in contentBody) { }

		public ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token)
		{
			try
			{
				var jsonDocument = JsonDocument.Parse(Value);
				return new ValueTask<IObject>(Map(jsonDocument.RootElement));
			}
			catch (JsonException)  { }

			return new ValueTask<IObject>(new DataModelValue(Value));
		}

		private DataModelValue Map(JsonElement element)
		{
			return element.ValueKind switch
			{
					JsonValueKind.Undefined => DataModelValue.Undefined,
					JsonValueKind.Object => new DataModelValue(MapObject(element)),
					JsonValueKind.Array => new DataModelValue(MapArray(element)),
					JsonValueKind.String => new DataModelValue(element.GetString()),
					JsonValueKind.Number => new DataModelValue(element.GetDouble()),
					JsonValueKind.True => new DataModelValue(true),
					JsonValueKind.False => new DataModelValue(false),
					JsonValueKind.Null => DataModelValue.Null,
					_ => throw new ArgumentOutOfRangeException()
			};
		}

		private DataModelObject MapObject(JsonElement element)
		{
			var dataModelObject = new DataModelObject();

			foreach (var jsonProperty in element.EnumerateObject())
			{
				dataModelObject[jsonProperty.Name] = Map(jsonProperty.Value);
			}

			return dataModelObject;
		}

		private DataModelArray MapArray(JsonElement element)
		{
			var dataModelArray = new DataModelArray();

			foreach (var jsonElement in element.EnumerateArray())
			{
				dataModelArray.Add(Map(jsonElement));
			}

			return dataModelArray;
		}
	}
}