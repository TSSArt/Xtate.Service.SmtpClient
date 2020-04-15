using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class JsonSerializationTest
	{
		[TestMethod]
		public void DeserializationJsonTest()
		{
			const string json1 = @"
{
""name"":""val"",
  ""data"": {
    ""status"": ""ok"",   
    ""status1"": ""ok1"",
    ""parameters"": {
		""response"": ""0378658708""
    }
  }
}";
			dynamic dataModelValue = DataModelConverter.FromJson(json1);

			Assert.AreEqual("ok", dataModelValue.data.status);
			Assert.IsInstanceOfType(dataModelValue.data.parameters.response, typeof(string));
		}

		[TestMethod]
		public void StringSerializationTest()
		{
			// arrange
			var val = (DataModelValue) "val";

			// act
			var json = DataModelConverter.ToJson(val);

			// assert
			Assert.AreEqual(expected: "\"val\"", json);
		}

		[TestMethod]
		public void CycleReferenceTest()
		{
			// arrange
			var dict = new DataModelObject();
			dict["self"] = dict;

			// act => assert
			Assert.ThrowsException<JsonException>(() => DataModelConverter.ToJson(dict));
		}

		[TestMethod]
		public void UndefinedFailTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;

			// act => assert
			Assert.ThrowsException<JsonException>(() => DataModelConverter.ToJson(undefined));
		}

		[TestMethod]
		public void UndefinedInObjectFailTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var obj = new DataModelObject { ["undef"] = undefined };

			// act
			var json = DataModelConverter.ToJson(obj);

			// assert
			Assert.AreEqual(expected: "{}", json);
		}

		[TestMethod]
		public void UndefinedInArrayFailTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var arr = new DataModelArray { undefined };

			// act => assert
			Assert.ThrowsException<JsonException>(() => DataModelConverter.ToJson(arr));
		}

		[TestMethod]
		public void UndefinedToNullTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;

			// act
			var json = DataModelConverter.ToJson(undefined, DataModelConverterOptions.UndefinedToNull);

			// assert
			Assert.AreEqual(expected: "null", json);
		}
		
		[TestMethod]
		public void UndefinedInObjectToNullTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var obj = new DataModelObject { ["undef"] = undefined };

			// act
			var json = DataModelConverter.ToJson(obj, DataModelConverterOptions.UndefinedToNull);

			// assert
			Assert.AreEqual(expected: "{\"undef\":null}", json);
		}
		
		[TestMethod]
		public void UndefinedInArrayToNullTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var arr = new DataModelArray { undefined };

			// act
			var json = DataModelConverter.ToJson(arr, DataModelConverterOptions.UndefinedToNull);

			// assert
			Assert.AreEqual(expected: "[null]", json);
		}

		[TestMethod]
		public void UndefinedToSkipTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;

			// act
			var json = DataModelConverter.ToJson(undefined, DataModelConverterOptions.UndefinedToSkip);

			// assert
			Assert.AreEqual(expected: "", json);
		}
		
		[TestMethod]
		public void UndefinedInObjectToSkipTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var obj = new DataModelObject { ["undef"] = undefined };

			// act
			var json = DataModelConverter.ToJson(obj, DataModelConverterOptions.UndefinedToSkip);

			// assert
			Assert.AreEqual(expected: "{}", json);
		}
		
		[TestMethod]
		public void UndefinedInArrayToSkipTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var arr = new DataModelArray { undefined };

			// act
			var json = DataModelConverter.ToJson(arr, DataModelConverterOptions.UndefinedToSkip);

			// assert
			Assert.AreEqual(expected: "[]", json);
		}

		[TestMethod]
		public void UndefinedToSkipOrNullTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;

			// act
			var json = DataModelConverter.ToJson(undefined, DataModelConverterOptions.UndefinedToSkipOrNull);

			// assert
			Assert.AreEqual(expected: "null", json);
		}
		
		[TestMethod]
		public void UndefinedInObjectToSkipOrNullTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var obj = new DataModelObject { ["undef"] = undefined };

			// act
			var json = DataModelConverter.ToJson(obj, DataModelConverterOptions.UndefinedToSkipOrNull);

			// assert
			Assert.AreEqual(expected: "{}", json);
		}
		
		[TestMethod]
		public void UndefinedInArrayToSkipOrNullTest()
		{
			// arrange
			var undefined = DataModelValue.Undefined;
			var arr = new DataModelArray { undefined };

			// act
			var json = DataModelConverter.ToJson(arr, DataModelConverterOptions.UndefinedToSkipOrNull);

			// assert
			Assert.AreEqual(expected: "[null]", json);
		}
	}
}