using System;

namespace Xtate
{
	internal sealed class DataModelArrayPersistingController : DataModelPersistingController
	{
		private readonly Bucket                    _bucket;
		private readonly DataModelArray            _dataModelArray;
		private readonly DataModelReferenceTracker _referenceTracker;
		private          int                       _record;

		public DataModelArrayPersistingController(Bucket bucket, DataModelReferenceTracker referenceTracker, DataModelArray dataModelArray)
		{
			_bucket = bucket;
			_referenceTracker = referenceTracker ?? throw new ArgumentNullException(nameof(referenceTracker));
			_dataModelArray = dataModelArray ?? throw new ArgumentNullException(nameof(dataModelArray));

			Restore(out var shrink);

			if (shrink)
			{
				Shrink();
			}

			dataModelArray.Changed += OnChanged;
		}

		private void Restore(out bool shrink)
		{
			shrink = _dataModelArray.Length > 0;
			while (true)
			{
				var recordBucket = _bucket.Nested(_record);

				if (!recordBucket.TryGet(Key.Operation, out Key operation))
				{
					break;
				}

				switch (operation)
				{
					case Key.Set when recordBucket.TryGet(Key.Index, out int index):
					{
						var dataModelValue = recordBucket.GetDataModelValue(_referenceTracker, _dataModelArray[index]);
						recordBucket.TryGet(Key.Access, out DataModelAccess access);
						if (_dataModelArray.SetInternal(index, new DataModelDescriptor(dataModelValue, access), throwOnDeny: false))
						{
							_referenceTracker.AddReference(dataModelValue);
						}

						break;
					}

					case Key.Insert when recordBucket.TryGet(Key.Index, out int index):
					{
						var dataModelValue = recordBucket.GetDataModelValue(_referenceTracker, baseValue: default);
						recordBucket.TryGet(Key.Access, out DataModelAccess access);
						if (_dataModelArray.InsertInternal(index, new DataModelDescriptor(dataModelValue, access), throwOnDeny: false))
						{
							_referenceTracker.AddReference(dataModelValue);
						}

						break;
					}

					case Key.Remove when recordBucket.TryGet(Key.Index, out int index):
					{
						shrink = true;
						var dataModelValue = _dataModelArray[index];
						if (_dataModelArray.RemoveAtInternal(index, throwOnDeny: false))
						{
							_referenceTracker.RemoveReference(dataModelValue);
						}

						break;
					}

					case Key.SetLength when recordBucket.TryGet(Key.Index, out int length):
					{
						shrink = length < _dataModelArray.Length;

						if (_dataModelArray.CanSetLength(length))
						{
							for (var i = length; i < _dataModelArray.Length; i ++)
							{
								_referenceTracker.RemoveReference(_dataModelArray[i]);
							}

							_dataModelArray.SetLengthInternal(length);
						}

						break;
					}

					default:
						Infrastructure.UnexpectedValue();

						break;
				}

				_record ++;
			}
		}

		private void Shrink()
		{
			_bucket.RemoveSubtree(Bucket.RootKey);

			if (_dataModelArray.Access != DataModelAccess.Writable)
			{
				_bucket.Add(Key.Access, _dataModelArray.Access);
			}

			_record = 0;
			for (var i = 0; i < _dataModelArray.Length; i ++)
			{
				var descriptor = _dataModelArray.GetDescriptor(i);
				if (!descriptor.Value.IsUndefined() || descriptor.Access != DataModelAccess.Writable)
				{
					var recordBucket = _bucket.Nested(_record ++);
					recordBucket.Add(Key.Operation, Key.Set);
					recordBucket.Add(Key.Index, i);

					if (descriptor.Access != DataModelAccess.Writable)
					{
						recordBucket.Add(Key.Access, descriptor.Access);
					}

					recordBucket.SetDataModelValue(_referenceTracker, descriptor.Value);
				}
			}
		}

		private void OnChanged(DataModelArray.ChangedAction action, int index, DataModelDescriptor descriptor)
		{
			switch (action)
			{
				case DataModelArray.ChangedAction.Set:
				{
					var recordBucket = _bucket.Nested(_record ++);
					recordBucket.Add(Key.Operation, Key.Set);
					recordBucket.Add(Key.Index, index);

					if (descriptor.Access != DataModelAccess.Writable)
					{
						recordBucket.Add(Key.Access, descriptor.Access);
					}

					_referenceTracker.AddReference(descriptor.Value);
					recordBucket.SetDataModelValue(_referenceTracker, descriptor.Value);
					break;
				}
				case DataModelArray.ChangedAction.Remove:
				{
					_referenceTracker.RemoveReference(descriptor.Value);
					if (_dataModelArray.Length > 1)
					{
						var recordBucket = _bucket.Nested(_record ++);
						recordBucket.Add(Key.Operation, Key.Remove);
						recordBucket.Add(Key.Index, index);
					}
					else
					{
						_record = 0;
						_bucket.RemoveSubtree(Bucket.RootKey);
					}

					break;
				}
				case DataModelArray.ChangedAction.Insert:
				{
					var recordBucket = _bucket.Nested(_record ++);
					recordBucket.Add(Key.Operation, Key.Insert);
					recordBucket.Add(Key.Index, index);

					if (descriptor.Access != DataModelAccess.Writable)
					{
						recordBucket.Add(Key.Access, descriptor.Access);
					}

					_referenceTracker.AddReference(descriptor.Value);
					recordBucket.SetDataModelValue(_referenceTracker, descriptor.Value);
					break;
				}
				case DataModelArray.ChangedAction.Clear:
				{
					foreach (var item in _dataModelArray)
					{
						_referenceTracker.RemoveReference(item);
					}

					_record = 0;
					_bucket.RemoveSubtree(Bucket.RootKey);
					break;
				}
				case DataModelArray.ChangedAction.SetLength:
				{
					if (index < _dataModelArray.Length)
					{
						for (var i = index; i < _dataModelArray.Length; i ++)
						{
							_referenceTracker.RemoveReference(_dataModelArray[i]);
						}
					}

					var recordBucket = _bucket.Nested(_record ++);
					recordBucket.Add(Key.Operation, Key.SetLength);
					recordBucket.Add(Key.Index, index);
					break;
				}
				default: throw new ArgumentOutOfRangeException(nameof(action), action, message: null);
			}
		}

		public override void Dispose()
		{
			_dataModelArray.Changed -= OnChanged;

			base.Dispose();
		}
	}
}