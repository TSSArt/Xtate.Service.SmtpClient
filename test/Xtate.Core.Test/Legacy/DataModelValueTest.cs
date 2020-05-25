using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xtate.Core.Test.Legacy
{
	[TestClass]
	[SuppressMessage(category: "ReSharper", checkId: "ExpressionIsAlwaysNull")]
	public class DataModelValueTest
	{
		[TestMethod]
		public void DataModelObjectNullTest()
		{
			// arrange
			DataModelObject nullVal = null!;

			// act
			var v = (DataModelValue) nullVal;

			// assert
			Assert.AreEqual(DataModelValue.Null, v);
			Assert.AreEqual(expected: null, v.AsNullableObject());
		}

		[TestMethod]
		public void DataModelArrayNullTest()
		{
			// arrange
			DataModelArray nullVal = null!;

			// act
			var v = (DataModelValue) nullVal;

			// assert
			Assert.AreEqual(DataModelValue.Null, v);
			Assert.AreEqual(expected: null, v.AsNullableArray());
		}

		[TestMethod]
		public void DataModelStringNullTest()
		{
			// arrange
			string nullVal = null!;

			// act
			var v = (DataModelValue) nullVal;

			// assert
			Assert.AreEqual(DataModelValue.Null, v);
			Assert.AreEqual(expected: null, v.AsNullableString());
		}

		[TestMethod]
		public void EqualityInequalityTest()
		{
			Assert.AreEqual(expected: default, new DataModelValue());
			Assert.AreEqual(DataModelValue.Null, DataModelValue.Null);
			Assert.AreNotEqual(DataModelValue.Null, actual: default);
			Assert.AreNotEqual(notExpected: default, DataModelValue.Null);
		}

		[TestMethod]
		public void TypesTest()
		{
			Assert.AreEqual(DataModelValueType.Undefined, default(DataModelValue).Type);
			Assert.AreEqual(DataModelValueType.Undefined, new DataModelValue().Type);
			Assert.AreEqual(DataModelValueType.Null, DataModelValue.Null.Type);
			Assert.AreEqual(DataModelValueType.String, DataModelValue.FromString("str").Type);
			Assert.AreEqual(DataModelValueType.Boolean, DataModelValue.FromBoolean(false).Type);
			Assert.AreEqual(DataModelValueType.Number, DataModelValue.FromDouble(0).Type);
			Assert.AreEqual(DataModelValueType.DateTime, DataModelValue.FromDateTimeOffset(DateTimeOffset.MinValue).Type);
			Assert.AreEqual(DataModelValueType.Object, DataModelValue.FromDataModelObject(new DataModelObject()).Type);
			Assert.AreEqual(DataModelValueType.Array, DataModelValue.FromDataModelArray(new DataModelArray()).Type);
		}

		[TestMethod]
		public void FromObjectDictionaryCycleRefTest()
		{
			// arrange
			var dict = new Dictionary<string, object>();
			dict["self"] = dict;

			// act
			var dst = DataModelValue.FromObject(dict);

			// assert
			Assert.AreSame(dst.AsObject(), dst.AsObject()["self"].AsObject());
		}

		[TestMethod]
		public void FromObjectArrayCycleRefTest()
		{
			// arrange
			var arr = new object[1];
			arr[0] = arr;

			// act
			var dst = DataModelValue.FromObject(arr);

			Assert.AreSame(dst.AsArray(), dst.AsArray()[0].AsArray());
		}

		[TestMethod]
		public void DeepCloneDictionaryCycleRefTest()
		{
			// arrange
			var dict = new DataModelObject();
			dict["self"] = dict;
			var src = (DataModelValue) dict;

			// act
			var dst = src.CloneAsWritable();

			// assert
			Assert.AreSame(dst.AsObject(), dst.AsObject()["self"].AsObject());
		}

		[TestMethod]
		public void DeepCloneArrayCycleRefTest()
		{
			// arrange
			var arr = new DataModelArray();
			arr[0] = arr;
			var src = (DataModelValue) arr;

			// act
			var dst = src.CloneAsWritable();

			// assert
			Assert.AreSame(dst.AsArray(), dst.AsArray()[0].AsArray());
		}

		[TestMethod]
		public void MakeDeepConstantMakeDeepReadOnlyDictionaryCycleRefTest()
		{
			// arrange
			var dict = new DataModelObject();
			dict["self"] = dict;
			var src = (DataModelValue) dict;

			// act
			src.MakeDeepConstant();

			// assert
			Assert.AreSame(src.AsObject(), src.AsObject()["self"].AsObject());
		}

		[TestMethod]
		public void MakeDeepConstantMakeDeepReadOnlyArrayCycleRefTest()
		{
			// arrange
			var arr = new DataModelArray();
			arr[0] = arr;
			var src = (DataModelValue) arr;

			// act
			src.MakeDeepConstant();

			// assert
			Assert.AreSame(src.AsArray(), src.AsArray()[0].AsArray());
		}

		[TestMethod]
		public void AnonymousTypeTest()
		{
			// arrange
			var at = new { Key = "Name" };

			// act
			var v = DataModelValue.FromObject(at);

			// assert
			Assert.AreEqual(expected: "Name", v.AsObject()["Key"].AsString());
		}
	}
}