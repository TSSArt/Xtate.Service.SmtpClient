using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSSArt.StateMachine.Test.DevTests
{
	[TestClass]
	public class DataModelValueTest
	{
		[TestMethod]
		public void FromObjectDictionaryCycleRefTest()
		{
			var dict = new Dictionary<string, object>();

			dict["self"] = dict;
			dict["value"] = "str";

			var v = DataModelValue.FromObject(dict);

			Assert.AreEqual(expected: "str", v.AsObject()["value"]);
			Assert.AreEqual(expected: "str", v.AsObject()["self"].AsObject()["value"]);
			Assert.AreSame(v.AsObject(), v.AsObject()["self"].AsObject());
		}

		[TestMethod]
		public void FromObjectArrayCycleRefTest()
		{
			var arr = new object[2];

			arr[0] = arr;
			arr[1] = "str";

			var v = DataModelValue.FromObject(arr);

			Assert.AreEqual(expected: "str", v.AsArray()[1]);
			Assert.AreEqual(expected: "str", v.AsArray()[0].AsArray()[1]);
			Assert.AreSame(v.AsArray(), v.AsArray()[0].AsArray());
		}

		[TestMethod]
		public void DeepCloneDictionaryCycleRefTest()
		{
			var dict = new DataModelObject();

			dict["self"] = dict;
			dict["value"] = "str";

			var src = (DataModelValue) dict;

			var dst = src.CloneAsWritable();

			Assert.AreEqual(expected: "str", dst.AsObject()["value"]);
			Assert.AreEqual(expected: "str", dst.AsObject()["self"].AsObject()["value"]);
			Assert.AreSame(dst.AsObject(), dst.AsObject()["self"].AsObject());
		}

		[TestMethod]
		public void DeepCloneArrayCycleRefTest()
		{
			var arr = new DataModelArray();

			arr[0] = arr;
			arr[1] = "str";

			var src = (DataModelValue) arr;

			var dst = src.CloneAsWritable();

			Assert.AreEqual(expected: "str", dst.AsArray()[1]);
			Assert.AreEqual(expected: "str", dst.AsArray()[0].AsArray()[1]);
			Assert.AreSame(dst.AsArray(), dst.AsArray()[0].AsArray());
		}

		[TestMethod]
		public void MakeDeepConstantMakeDeepReadOnlyDictionaryCycleRefTest()
		{
			var dict = new DataModelObject();

			dict["self"] = dict;
			dict["value"] = "str";

			var src = (DataModelValue) dict;

			src.MakeDeepConstant();

			Assert.AreEqual(expected: "str", src.AsObject()["value"]);
			Assert.AreEqual(expected: "str", src.AsObject()["self"].AsObject()["value"]);
			Assert.AreSame(src.AsObject(), src.AsObject()["self"].AsObject());
		}

		[TestMethod]
		public void MakeDeepConstantMakeDeepReadOnlyArrayCycleRefTest()
		{
			var arr = new DataModelArray();

			arr[0] = arr;
			arr[1] = "str";

			var src = (DataModelValue) arr;

			src.MakeDeepConstant();

			Assert.AreEqual(expected: "str", src.AsArray()[1]);
			Assert.AreEqual(expected: "str", src.AsArray()[0].AsArray()[1]);
			Assert.AreSame(src.AsArray(), src.AsArray()[0].AsArray());
		}
	}
}