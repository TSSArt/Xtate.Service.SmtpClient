using System;

namespace TSSArt.StateMachine
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

			var shrink = dataModelObject.Properties.Count > 0;
			while (true)
			{
				var recordBucket = bucket.Nested(_record);

				if (!recordBucket.TryGet(Key.Operation, out Key operation))
				{
					break;
				}

				switch (operation)
				{
					case Key.Set when recordBucket.TryGet(Key.Property, out string? property):
					{
						var dataModelValue = recordBucket.GetDataModelValue(referenceTracker, dataModelObject[property]);
						recordBucket.TryGet(Key.ReadOnly, out bool isReadOnly);
						dataModelObject.SetInternal(property, new DataModelDescriptor(dataModelValue, isReadOnly));
						referenceTracker.AddReference(dataModelValue);
						break;
					}

					case Key.Remove when recordBucket.TryGet(Key.Property, out string? property):
					{
						shrink = true;
						referenceTracker.RemoveReference(dataModelObject[property]);
						dataModelObject.RemoveInternal(property);
						break;
					}

					default:
						Infrastructure.UnexpectedValue();
						break;
				}

				_record ++;
			}

			if (shrink)
			{
				bucket.RemoveSubtree(Bucket.RootKey);
				if (dataModelObject.Access != DataModelAccess.Writable)
				{
					bucket.Add(Key.Access, dataModelObject.Access);
				}

				_record = 0;
				foreach (var property in dataModelObject.Properties)
				{
					var descriptor = dataModelObject.GetDescriptor(property);
					if (!descriptor.Value.IsUndefined() || descriptor.IsReadOnly)
					{
						var recordBucket = bucket.Nested(_record ++);
						recordBucket.Add(Key.Operation, Key.Set);
						recordBucket.Add(Key.Property, property);

						if (descriptor.IsReadOnly)
						{
							recordBucket.Add(Key.ReadOnly, value: true);
						}

						recordBucket.SetDataModelValue(referenceTracker, descriptor.Value);
					}
				}
			}

			dataModelObject.Changed += OnChanged;
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

					if (descriptor.IsReadOnly)
					{
						recordBucket.Add(Key.ReadOnly, value: true);
					}

					_referenceTracker.AddReference(descriptor.Value);
					recordBucket.SetDataModelValue(_referenceTracker, descriptor.Value);
					break;
				}
				case DataModelObject.ChangedAction.Remove:
				{
					_referenceTracker.RemoveReference(descriptor.Value);
					if (_dataModelObject.Properties.Count > 1)
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
		}
	}
}