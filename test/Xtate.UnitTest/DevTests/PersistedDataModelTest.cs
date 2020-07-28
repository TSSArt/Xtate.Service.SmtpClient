#region Copyright © 2019-2020 Sergii Artemenko
// 
// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
// 
#endregion

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.Persistence;

namespace Xtate.Test
{
	[TestClass]
	public class PersistedDataModelTest
	{
		private DataModelListPersistingController _arrayController = default!;
		private Bucket                            _bucket;
		private DataModelArray                    _dataModelArray          = default!;
		private DataModelObject                   _dataModelObject         = default!;
		private DataModelListPersistingController _objectController        = default!;
		private DataModelArray                    _restoredDataModelArray  = default!;
		private DataModelObject                   _restoredDataModelObject = default!;
		private DataModelReferenceTracker         _restoredTracker         = default!;
		private InMemoryStorage                   _storage                 = default!;
		private DataModelReferenceTracker         _tracker                 = default!;

		[TestInitialize]
		public void Initialize()
		{
			_storage = new InMemoryStorage(false);
			var bucket = new Bucket(_storage);
			_bucket = bucket.Nested("root");

			_tracker = new DataModelReferenceTracker(bucket.Nested("refs"));
			_restoredTracker = new DataModelReferenceTracker(bucket.Nested("refs"));

			_dataModelObject = new DataModelObject();
			_objectController = new DataModelListPersistingController(_bucket, _tracker, _dataModelObject);

			_restoredDataModelObject = new DataModelObject();

			_dataModelArray = new DataModelArray();
			_arrayController = new DataModelListPersistingController(_bucket, _tracker, _dataModelArray);

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
			using var controller = new DataModelListPersistingController(_bucket, _tracker, _restoredDataModelObject);

			Assert.AreEqual(expected: 0, _restoredDataModelObject.Count);
			Assert.AreEqual(expected: 0, _storage.GetTransactionLogSize());
		}

		[TestMethod]
		public void AddObjectTest()
		{
			_dataModelObject["b"] = new DataModelValue("ee");

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

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

			_dataModelObject.RemoveAll("b");

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(expected: 1, _restoredDataModelObject.Count);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 15, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void RemoveObjectAllTest()
		{
			_dataModelObject["a"] = new DataModelValue("qq");
			_dataModelObject["b"] = new DataModelValue("ee");

			_dataModelObject.RemoveAll("a");
			_dataModelObject.RemoveAll("b");

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(expected: 0, _restoredDataModelObject.Count);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 13, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void SubObjectTest()
		{
			var obj = new DataModelObject { ["t"] = new DataModelValue("test") };
			_dataModelObject["a"] = new DataModelValue(obj);

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.AreEqual(expected: "test", _restoredDataModelObject["a"].AsObject()["t"].AsString());
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 10, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void AddArrayTest()
		{
			_dataModelArray[0] = new DataModelValue("qq");

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
			Assert.AreEqual(expected: "qq", _restoredDataModelArray[0].AsString());
		}

		[TestMethod]
		public void RemoveArrayTest()
		{
			_dataModelArray[0] = new DataModelValue("qq");
			_dataModelArray[1] = new DataModelValue("e");

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
			Assert.AreEqual(expected: "qq", _restoredDataModelArray[0].AsString());
		}

		[TestMethod]
		public void RemoveArrayAllTest()
		{
			_dataModelArray[0] = new DataModelValue("qq");
			_dataModelArray[1] = new DataModelValue("e");

			_dataModelArray.RemoveAt(0);
			_dataModelArray.RemoveAt(0);

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 0, _restoredDataModelArray.Count);
		}

		[TestMethod]
		public void AddDoubleTest()
		{
			_dataModelArray[0] = new DataModelValue(1.2);

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
			Assert.AreEqual(expected: 1.2, _restoredDataModelArray[0].AsNumber());
		}

		[TestMethod]
		public void AddDateTimeTest()
		{
			_dataModelArray[0] = new DataModelValue(new DateTime(year: 2000, month: 1, day: 1));

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
			Assert.AreEqual(new DateTime(year: 2000, month: 1, day: 1), _restoredDataModelArray[0].AsDateTime().ToDateTime());
		}

		[TestMethod]
		public void AddBooleanTest()
		{
			_dataModelArray[0] = new DataModelValue(true);

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
			Assert.AreEqual(expected: true, _restoredDataModelArray[0].AsBoolean());
		}

		[TestMethod]
		public void AddStringTest()
		{
			_dataModelArray[0] = new DataModelValue("test");

			_dataModelArray.RemoveAt(1);

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
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

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
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

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 1, _restoredDataModelArray.Count);
			Assert.AreEqual(expected: "value", _restoredDataModelArray[0].AsArray()[0].AsString());
		}

		[TestMethod]
		public void ArrayClearTest()
		{
			_dataModelArray.Add(new DataModelValue(5));
			_dataModelArray.Clear();

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 0, _restoredDataModelArray.Count);
		}

		[TestMethod]
		public void ArrayInsertTest()
		{
			_dataModelArray.Add(new DataModelValue(5));
			_dataModelArray.Insert(index: 0, new DataModelValue(4));

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 2, _restoredDataModelArray.Count);
			Assert.AreEqual(expected: 4, _restoredDataModelArray[0].AsNumber());
			Assert.AreEqual(expected: 5, _restoredDataModelArray[1].AsNumber());
		}

		[TestMethod]
		public void ArraySetLengthTest()
		{
			_dataModelArray.Add(new DataModelValue(5));
			_dataModelArray.SetLength(5);

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 5, _restoredDataModelArray.Count);
			Assert.AreEqual(expected: 5, _restoredDataModelArray[0].AsNumber());
			Assert.AreEqual(DataModelValueType.Undefined, _restoredDataModelArray[4].Type);
		}

		[TestMethod]
		public void ArrayReadonlyStringPropTest()
		{
			var obj = new DataModelObject();

			obj.SetInternal(key: "t", caseInsensitive: false, new DataModelValue(value: "test"), DataModelAccess.ReadOnly);

			_dataModelObject["a"] = new DataModelValue(obj);

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.IsTrue(_restoredDataModelObject["a"].AsObject().TryGet(key: "t", caseInsensitive: false, out var entry) && entry.Access == DataModelAccess.ReadOnly);
			Assert.AreEqual(expected: false, _restoredDataModelObject["a"].AsObject().CanSet(key: "t", caseInsensitive: false));
			Assert.AreEqual(expected: true, _restoredDataModelObject.CanSet(key: "a", caseInsensitive: false));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 12, StorageTest.GetEntriesCount(_storage));
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

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.IsTrue(_restoredDataModelObject["a"].AsObject().TryGet(key: "t", caseInsensitive: false, out var entry) && entry.Access == DataModelAccess.Writable);
			Assert.AreEqual(expected: false, _restoredDataModelObject["a"].AsObject().CanSet(key: "t", caseInsensitive: false));
			Assert.AreEqual(expected: true, _restoredDataModelObject.CanSet(key: "a", caseInsensitive: false));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 12, StorageTest.GetEntriesCount(_storage));
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

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.IsTrue(_restoredDataModelObject["a"].AsArray().TryGet(index: 0, out var entry) && entry.Access == DataModelAccess.Writable);
			Assert.AreEqual(expected: false, _restoredDataModelObject["a"].AsArray().CanSet(index: 0));
			Assert.AreEqual(expected: true, _restoredDataModelObject.CanSet(key: "a", caseInsensitive: false));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 12, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void ArrayReadonlyObjectPropertyTest()
		{
			var obj = new DataModelObject
					  {
							  ["t"] = new DataModelValue("test")
					  };
			_dataModelObject.SetInternal(key: "a", caseInsensitive: false, new DataModelValue(obj), DataModelAccess.ReadOnly);

			using var controller = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);

			Assert.IsTrue(_restoredDataModelObject["a"].AsObject().TryGet(key: "t", caseInsensitive: false, out var entry) && entry.Access == DataModelAccess.Writable);
			Assert.AreEqual(expected: true, _restoredDataModelObject["a"].AsObject().CanSet(key: "t", caseInsensitive: false));
			Assert.AreEqual(expected: false, _restoredDataModelObject.CanSet(key: "a", caseInsensitive: false));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
			Assert.AreEqual(expected: 11, StorageTest.GetEntriesCount(_storage));
		}

		[TestMethod]
		public void ReferencesNewObjectTest()
		{
			var obj1 = new DataModelObject { ["prop1-rw"] = new DataModelValue("val1") };
			var root = new DataModelObject { ["obj1"] = new DataModelValue(obj1) };
			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, root);
		}

		[TestMethod]
		public void ReferencesObjectTest()
		{
			var obj1 = new DataModelObject
					   {
							   ["prop1-rw"] = new DataModelValue("val1")
					   };
			obj1.SetInternal(key: "prop1-ro", caseInsensitive: false, new DataModelValue("val1"), DataModelAccess.ReadOnly);

			var obj2 = new DataModelObject
					   {
							   ["prop2-rw"] = new DataModelValue("val1")
					   };
			obj1.SetInternal(key: "prop2-ro", caseInsensitive: false, new DataModelValue("val1"), DataModelAccess.ReadOnly);

			_dataModelObject["numeric-rw"] = new DataModelValue(11);
			obj1.SetInternal(key: "numeric-ro", caseInsensitive: false, new DataModelValue(22), DataModelAccess.ReadOnly);

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

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelObject);
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
			obj1.SetInternal(index: 0, key: null, new DataModelValue("val1"), DataModelAccess.ReadOnly);

			var obj2 = new DataModelArray
					   {
							   [1] = new DataModelValue("val1")
					   };
			obj1.SetInternal(index: 0, key: null, new DataModelValue("val1"), DataModelAccess.ReadOnly);

			_dataModelArray[0] = new DataModelValue(11);
			obj1.SetInternal(index: 1, key: null, new DataModelValue(22), DataModelAccess.ReadOnly);
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

			var _ = new DataModelListPersistingController(_bucket, _restoredTracker, _restoredDataModelArray);
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

			Assert.AreEqual(expected: "prop1-rw", new Bucket(_storage).Nested("refs").Nested(0).Nested(0).GetString(Key.Key));
			Assert.IsTrue(new Bucket(_storage).Nested("refs").Nested(0).Nested(0).TryGet(Key.Key, out string _));
			_dataModelObject["obj1a"] = default;
			Assert.IsFalse(new Bucket(_storage).Nested("refs").Nested(0).Nested(0).TryGet(Key.Key, out string _));
			Console.WriteLine(StorageTest.Dump(_storage, Environment.NewLine, hex: true));
		}
	}
}