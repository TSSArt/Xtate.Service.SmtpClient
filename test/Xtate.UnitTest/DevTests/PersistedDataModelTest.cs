using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xtate.Test
{
	[TestClass]
	public class PersistedDataModelTest
	{
		private DataModelArrayPersistingController  _arrayController = default!;
		private Bucket                              _bucket;
		private DataModelArray                      _dataModelArray          = default!;
		private DataModelObject                     _dataModelObject         = default!;
		private DataModelObjectPersistingController _objectController        = default!;
		private DataModelArray                      _restoredDataModelArray  = default!;
		private DataModelObject                     _restoredDataModelObject = default!;
		private DataModelReferenceTracker           _restoredTracker         = default!;
		private InMemoryStorage                     _storage                 = default!;
		private DataModelReferenceTracker           _tracker                 = default!;

		[TestInitialize]
		public void Initialize()
		{
			_storage = new InMemoryStorage(false);
			var bucket = new Bucket(_storage);
			_bucket = bucket.Nested("root");

			_tracker = new DataModelReferenceTracker(bucket.Nested("refs"));
			_restoredTracker = new DataModelReferenceTracker(bucket.Nested("refs"));

			_dataModelObject = new DataModelObject();
			_objectController = new DataModelObjectPersistingController(_bucket, _tracker, _dataModelObject);

			_restoredDataModelObject = new DataModelObject();

			_dataModelArray = new DataModelArray();
			_arrayController = new DataModelArrayPersistingController(_bucket, _tracker, _dataModelArray);

			_restoredDataModelArray = new DataModelArray();
		}

		[TestCleanup]
		public void Finalization()
		{
			_tracker.Dispose();
			_objectController.Dispose();
			_arrayController.Dispose();
		}

		[TestMethod]
		public void EmptyObjectTest()
		{
			using var controller = new DataModelObjectPersistingController(_bucket, _tracker, _restoredDataModelObject);

			Assert.AreEqual(expected: 0, _restoredDataModelObject.Count);
			Assert.AreEqual(expected: 0, _storage.GetTransactionLogSize());
		}

		[TestMethod]
		public void AddObjectTest()
		{
			_dataModelObject["b"] = new DataModelValue("ee");

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(expected: 1, _restoredDataModelObject.Count);
			Assert.AreEqual(expected: "ee", _restoredDataModelObject["b"].AsString());

			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 4, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void RemoveObjectTest()
		{
			_dataModelObject["a"] = new DataModelValue("qq");
			_dataModelObject["b"] = new DataModelValue("ee");

			_dataModelObject.Remove("b");

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(expected: 1, _restoredDataModelObject.Count);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 15, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void RemoveObjectAllTest()
		{
			_dataModelObject["a"] = new DataModelValue("qq");
			_dataModelObject["b"] = new DataModelValue("ee");

			_dataModelObject.Remove("a");
			_dataModelObject.Remove("b");

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(expected: 0, _restoredDataModelObject.Count);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 11, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void SubObjectTest()
		{
			var obj = new DataModelObject { ["t"] = new DataModelValue("test") };
			_dataModelObject["a"] = new DataModelValue(obj);

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(expected: "test", _restoredDataModelObject["a"].AsObject()["t"].AsString());
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 10, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void AddArrayTest()
		{
			_dataModelArray[0] = new DataModelValue("qq");

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: "qq", _restoredDataModelArray[0].AsString());
		}

		[TestMethod]
		public void RemoveArrayTest()
		{
			_dataModelArray[0] = new DataModelValue("qq");
			_dataModelArray[1] = new DataModelValue("e");

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: "qq", _restoredDataModelArray[0].AsString());
		}

		[TestMethod]
		public void RemoveArrayAllTest()
		{
			_dataModelArray[0] = new DataModelValue("qq");
			_dataModelArray[1] = new DataModelValue("e");

			_dataModelArray.RemoveAt(0);
			_dataModelArray.RemoveAt(0);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 0, _restoredDataModelArray.Length);
		}

		[TestMethod]
		public void AddDoubleTest()
		{
			_dataModelArray[0] = new DataModelValue(1.2);

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: 1.2, _restoredDataModelArray[0].AsNumber());
		}

		[TestMethod]
		public void AddDateTimeTest()
		{
			_dataModelArray[0] = new DataModelValue(new DateTime(year: 2000, month: 1, day: 1));

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(new DateTime(year: 2000, month: 1, day: 1), _restoredDataModelArray[0].AsDateTime().ToDateTime());
		}

		[TestMethod]
		public void AddBooleanTest()
		{
			_dataModelArray[0] = new DataModelValue(true);

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: true, _restoredDataModelArray[0].AsBoolean());
		}

		[TestMethod]
		public void AddStringTest()
		{
			_dataModelArray[0] = new DataModelValue("test");

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: "test", _restoredDataModelArray[0].AsString());
		}

		[TestMethod]
		public void AddSubObjectTest()
		{
			var obj = new DataModelObject
					  {
							  ["prop"] = new DataModelValue("value")
					  };

			_dataModelArray[0] = new DataModelValue(obj);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: "value", _restoredDataModelArray[0].AsObject()["prop"].AsString());
		}

		[TestMethod]
		public void AddSubArrayTest()
		{
			var obj = new DataModelArray
					  {
							  [0] = new DataModelValue("value")
					  };

			_dataModelArray[0] = new DataModelValue(obj);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: "value", _restoredDataModelArray[0].AsArray()[0].AsString());
		}

		[TestMethod]
		public void ArrayClearTest()
		{
			_dataModelArray.Add(new DataModelValue(5));
			_dataModelArray.Clear();

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 0, _restoredDataModelArray.Length);
		}

		[TestMethod]
		public void ArrayInsertTest()
		{
			_dataModelArray.Add(new DataModelValue(5));
			_dataModelArray.Insert(index: 0, new DataModelValue(4));

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 2, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: 4, _restoredDataModelArray[0].AsNumber());
			Assert.AreEqual(expected: 5, _restoredDataModelArray[1].AsNumber());
		}

		[TestMethod]
		public void ArraySetLengthTest()
		{
			_dataModelArray.Add(new DataModelValue(5));
			_dataModelArray.SetLength(5);

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 5, _restoredDataModelArray.Length);
			Assert.AreEqual(expected: 5, _restoredDataModelArray[0].AsNumber());
			Assert.AreEqual(DataModelValueType.Undefined, _restoredDataModelArray[4].Type);
		}

		[TestMethod]
		public void ArrayReadonlyStringPropTest()
		{
			var obj = new DataModelObject();

			obj.SetInternal(property: "t", new DataModelDescriptor(new DataModelValue(value: "test"), DataModelAccess.ReadOnly));

			_dataModelObject["a"] = new DataModelValue(obj);

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(DataModelAccess.ReadOnly, _restoredDataModelObject["a"].AsObject().GetDescriptor("t").Access);
			Assert.AreEqual(expected: false, _restoredDataModelObject["a"].AsObject().CanSet("t"));
			Assert.AreEqual(expected: true, _restoredDataModelObject.CanSet("a"));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 11, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void ArrayReadonlyObjectTest()
		{
			var obj = new DataModelObject
					  {
							  ["t"] = new DataModelValue("test")
					  };
			obj.MakeReadOnly();
			_dataModelObject["a"] = new DataModelValue(obj);

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(DataModelAccess.Writable, _restoredDataModelObject["a"].AsObject().GetDescriptor("t").Access);
			Assert.AreEqual(expected: false, _restoredDataModelObject["a"].AsObject().CanSet("t"));
			Assert.AreEqual(expected: true, _restoredDataModelObject.CanSet("a"));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 11, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void ArrayReadonlyArrayTest()
		{
			var array = new DataModelArray
						{
								[0] = new DataModelValue("test")
						};
			array.MakeReadOnly();
			_dataModelObject["a"] = new DataModelValue(array);

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(DataModelAccess.Writable, _restoredDataModelObject["a"].AsArray().GetDescriptor(0).Access);
			Assert.AreEqual(expected: false, _restoredDataModelObject["a"].AsArray().CanSet(index: 0));
			Assert.AreEqual(expected: true, _restoredDataModelObject.CanSet("a"));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 11, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void ArrayReadonlyObjectPropertyTest()
		{
			var obj = new DataModelObject
					  {
							  ["t"] = new DataModelValue("test")
					  };
			_dataModelObject.SetInternal(property: "a", new DataModelDescriptor(new DataModelValue(obj), DataModelAccess.ReadOnly));

			using var controller = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(DataModelAccess.Writable, _restoredDataModelObject["a"].AsObject().GetDescriptor("t").Access);
			Assert.AreEqual(expected: true, _restoredDataModelObject["a"].AsObject().CanSet("t"));
			Assert.AreEqual(expected: false, _restoredDataModelObject.CanSet("a"));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 11, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void ReferencesNewObjectTest()
		{
			var obj1 = new DataModelObject { ["prop1-rw"] = new DataModelValue("val1") };
			var root = new DataModelObject { ["obj1"] = new DataModelValue(obj1) };
			var _ = new DataModelObjectPersistingController(_bucket, _restoredTracker, root);
		}

		[TestMethod]
		public void ReferencesObjectTest()
		{
			var obj1 = new DataModelObject
					   {
							   ["prop1-rw"] = new DataModelValue("val1")
					   };
			obj1.SetInternal(property: "prop1-ro", new DataModelDescriptor(new DataModelValue("val1"), DataModelAccess.ReadOnly));

			var obj2 = new DataModelObject
					   {
							   ["prop2-rw"] = new DataModelValue("val1")
					   };
			obj1.SetInternal(property: "prop2-ro", new DataModelDescriptor(new DataModelValue("val1"), DataModelAccess.ReadOnly));

			_dataModelObject["numeric-rw"] = new DataModelValue(11);
			obj1.SetInternal(property: "numeric-ro", new DataModelDescriptor(new DataModelValue(22), DataModelAccess.ReadOnly));

			_dataModelObject["obj1a"] = new DataModelValue(obj1);
			_dataModelObject["obj1b"] = new DataModelValue(obj1);
			_dataModelObject["obj2a"] = new DataModelValue(obj2);
			_dataModelObject["obj2b"] = new DataModelValue(obj2);

			_dataModelObject["obj1c"] = new DataModelValue(obj1);
			_dataModelObject["obj1c"] = DataModelValue.Null;

			obj1["extra1"] = new DataModelValue("val-extra1");
			obj2["extra2"] = new DataModelValue("val-extra2");

			Assert.AreSame(obj1, _dataModelObject["obj1a"].AsObject());
			Assert.AreSame(obj1, _dataModelObject["obj1b"].AsObject());
			Assert.AreSame(obj2, _dataModelObject["obj2a"].AsObject());
			Assert.AreSame(obj2, _dataModelObject["obj2b"].AsObject());
			Assert.AreEqual(expected: "val-extra1", _dataModelObject["obj1a"].AsObject()["extra1"].AsString());
			Assert.AreEqual(expected: "val-extra1", _dataModelObject["obj1b"].AsObject()["extra1"].AsString());
			Assert.AreEqual(expected: "val-extra2", _dataModelObject["obj2a"].AsObject()["extra2"].AsString());
			Assert.AreEqual(expected: "val-extra2", _dataModelObject["obj2b"].AsObject()["extra2"].AsString());

			var _ = new DataModelObjectPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreSame(_restoredDataModelObject["obj1a"].AsObject(), _restoredDataModelObject["obj1b"].AsObject());
			Assert.AreSame(_restoredDataModelObject["obj2a"].AsObject(), _restoredDataModelObject["obj2b"].AsObject());
			Assert.AreEqual(expected: "val-extra1", _restoredDataModelObject["obj1a"].AsObject()["extra1"].AsString());
			Assert.AreEqual(expected: "val-extra1", _restoredDataModelObject["obj1b"].AsObject()["extra1"].AsString());
			Assert.AreEqual(expected: "val-extra2", _restoredDataModelObject["obj2a"].AsObject()["extra2"].AsString());
			Assert.AreEqual(expected: "val-extra2", _restoredDataModelObject["obj2b"].AsObject()["extra2"].AsString());
		}

		[TestMethod]
		public void ReferencesArrayTest()
		{
			var obj1 = new DataModelArray
					   {
							   [1] = new DataModelValue("val1")
					   };
			obj1.SetInternal(index: 0, new DataModelDescriptor(new DataModelValue("val1"), DataModelAccess.ReadOnly));

			var obj2 = new DataModelArray
					   {
							   [1] = new DataModelValue("val1")
					   };
			obj1.SetInternal(index: 0, new DataModelDescriptor(new DataModelValue("val1"), DataModelAccess.ReadOnly));

			_dataModelArray[0] = new DataModelValue(11);
			obj1.SetInternal(index: 1, new DataModelDescriptor(new DataModelValue(22), DataModelAccess.ReadOnly));
			_dataModelArray[2] = new DataModelValue(obj1);
			_dataModelArray[3] = new DataModelValue(obj1);
			_dataModelArray[4] = new DataModelValue(obj2);
			_dataModelArray[5] = new DataModelValue(obj2);

			obj1[2] = new DataModelValue("val-extra1");
			obj2[3] = new DataModelValue("val-extra2");

			_dataModelArray[6] = new DataModelValue(obj1);
			_dataModelArray[6] = DataModelValue.Null;

			Assert.AreSame(obj1, _dataModelArray[2].AsArray());
			Assert.AreSame(obj1, _dataModelArray[3].AsArray());
			Assert.AreSame(obj2, _dataModelArray[4].AsArray());
			Assert.AreSame(obj2, _dataModelArray[5].AsArray());
			Assert.AreEqual(expected: "val-extra1", _dataModelArray[2].AsArray()[2].AsString());
			Assert.AreEqual(expected: "val-extra1", _dataModelArray[3].AsArray()[2].AsString());
			Assert.AreEqual(expected: "val-extra2", _dataModelArray[4].AsArray()[3].AsString());
			Assert.AreEqual(expected: "val-extra2", _dataModelArray[5].AsArray()[3].AsString());

			var _ = new DataModelArrayPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreSame(_restoredDataModelArray[2].AsArray(), _restoredDataModelArray[3].AsArray());
			Assert.AreSame(_restoredDataModelArray[4].AsArray(), _restoredDataModelArray[5].AsArray());
			Assert.AreEqual(expected: "val-extra1", _restoredDataModelArray[2].AsArray()[2].AsString());
			Assert.AreEqual(expected: "val-extra1", _restoredDataModelArray[3].AsArray()[2].AsString());
			Assert.AreEqual(expected: "val-extra2", _restoredDataModelArray[4].AsArray()[3].AsString());
			Assert.AreEqual(expected: "val-extra2", _restoredDataModelArray[5].AsArray()[3].AsString());
		}

		[TestMethod]
		public void ReferencesRemovedTest()
		{
			var obj1 = new DataModelObject { ["prop1-rw"] = new DataModelValue("val1") };

			_dataModelObject["obj1a"] = new DataModelValue(obj1);

			Assert.AreEqual(expected: "prop1-rw", new Bucket(_storage).Nested("refs").Nested(0).Nested(0).GetString(Key.Property));
			Assert.IsTrue(new Bucket(_storage).Nested("refs").Nested(0).Nested(0).TryGet(Key.Property, out string _));
			_dataModelObject["obj1a"] = default;
			Assert.IsFalse(new Bucket(_storage).Nested("refs").Nested(0).Nested(0).TryGet(Key.Property, out string _));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
		}
	}
}