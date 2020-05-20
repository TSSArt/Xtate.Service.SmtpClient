namespace Xtate
{
	public readonly struct DataModelDescriptor
	{
		public DataModelDescriptor(DataModelValue value, DataModelAccess access = DataModelAccess.Writable)
		{
			Value = value;
			Access = access;
		}

		public DataModelValue Value { get; }

		public DataModelAccess Access { get; }
	}
}