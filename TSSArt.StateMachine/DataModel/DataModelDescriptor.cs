namespace TSSArt.StateMachine
{
	public readonly struct DataModelDescriptor
	{
		public DataModelDescriptor(DataModelValue value, bool isReadOnly = false)
		{
			Value = value;
			IsReadOnly = isReadOnly;
		}

		public DataModelValue Value { get; }

		public bool IsReadOnly { get; }
	}
}