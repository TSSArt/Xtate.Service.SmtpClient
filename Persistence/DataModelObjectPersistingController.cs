using System;

namespace Xtate
{
	internal sealed class DataModelObjectPersistingController : DataModelPersistingController
	{
		private readonly Bucket                    _bucket;
		private readonly DataModelObject           _dataModelObject;
		private readonly DataModelReferenceTracker _referenceTracker;
		private          int                       _record;

		public DataModelObjectPersistingController(Bucket bucket, DataModelReferenceTracker referenceTracker, DataModelObject dataModelObject)
		{
			_bucket = bucket;
			_referenceTracker = referenceTracker ?? throw new ArgumentNullException(nameof(referenceTracker));
			_dataModelObject = dataModelObject ?? throw new ArgumentNullException(nameof(dataModelObject));

			Restore(out var shrink);

			if (shrink)
			{
				Shrink();
			}

			dataModelObject.Changed += OnChanged;
		}

		private void Restore(out bool shrink)
		{
			shrink = _dataModelObject.Count > 0;
			while (true)
			{
				var recordBucket = _bucket.Nested(_record);

				if (!recordBucket.TryGet(Key.Operation, out Key operation))
				{
					break;
				}

				switch (operation)
				{
					case Key.Set when recordBucket.TryGet(Key.Property, out string? property):
					{
						var dataModelValue = recordBucket.GetDataModelValue(_referenceTracker, _dataModelObject[property]);
						recordBucket.TryGet(Key.Access, out DataModelAccess access);
						if (_dataModelObject.SetInternal(property, new DataModelDescriptor(dataModelValue, access), throwOnDeny: false))
						{
							_referenceTracker.AddReference(dataModelValue);
						}

						break;
					}

					case Key.Remove when recordBucket.TryGet(Key.Property, out string? property):
					{
						shrink = true;
						var dataModelValue = _dataModelObject[property];
						if (_dataModelObject.RemoveInternal(property, throwOnDeny: false))
						{
							_referenceTracker.RemoveReference(dataModelValue);
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
			if (_dataModelObject.Access != DataModelAccess.Writable)
			{
				_bucket.Add(Key.Access, _dataModelObject.Access);
			}

			_record = 0;
			foreach (var property in _dataModelObject.Properties)
			{
				var descriptor = _dataModelObject.GetDescriptor(property);
				if (!descriptor.Value.IsUndefined() || descriptor.Access != DataModelAccess.Writable)
				{
					var recordBucket = _bucket.Nested(_record ++);
					recordBucket.Add(Key.Operation, Key.Set);
					recordBucket.Add(Key.Property, property);

					if (descriptor.Access != DataModelAccess.Writable)
					{
						recordBucket.Add(Key.Access, descriptor.Access);
					}

					recordBucket.SetDataModelValue(_referenceTracker, descriptor.Value);
				}
			}
		}

		private void OnChanged(DataModelObject.ChangedAction action, string property, DataModelDescriptor descriptor)
		{
			switch (action)
			{
				case DataModelObject.ChangedAction.Set:
				{
					var recordBucket = _bucket.Nested(_record ++);
					recordBucket.Add(Key.Operation, Key.Set);
					recordBucket.Add(Key.Property, property);

					if (descriptor.Access != DataModelAccess.Writable)
					{
						recordBucket.Add(Key.Access, descriptor.Access);
					}

					_referenceTracker.AddReference(descriptor.Value);
					recordBucket.SetDataModelValue(_referenceTracker, descriptor.Value);
					break;
				}
				case DataModelObject.ChangedAction.Remove:
				{
					_referenceTracker.RemoveReference(descriptor.Value);
					if (_dataModelObject.Count > 1)
					{
						var recordBucket = _bucket.Nested(_record ++);
						recordBucket.Add(Key.Operation, Key.Remove);
						recordBucket.Add(Key.Property, property);
					}
					else
					{
						_record = 0;
						_bucket.RemoveSubtree(Bucket.RootKey);
					}

					break;
				}
				default: throw new ArgumentOutOfRangeException(nameof(action), action, message: null);
			}
		}

		public override void Dispose()
		{
			_dataModelObject.Changed -= OnChanged;

			base.Dispose();
		}
	}
}