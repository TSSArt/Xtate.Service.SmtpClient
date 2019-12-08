using System;

namespace TSSArt.StateMachine
{
	public readonly struct DataModelDescriptor : IEquatable<DataModelDescriptor>
	{
		public DataModelDescriptor(DataModelValue value, bool isReadOnly = false)
		{
			Value = value;
			IsReadOnly = isReadOnly;
		}

		public DataModelValue Value { get; }

		public bool IsReadOnly { get; }

		public bool Equals(DataModelDescriptor other) => Value.Equals(other.Value);

		public override bool Equals(object obj) => obj is DataModelDescriptor other && Equals(other);

		public override int GetHashCode() => Value.GetHashCode();

		public static bool operator ==(DataModelDescriptor left, DataModelDescriptor right) => left.Equals(right);

		public static bool operator !=(DataModelDescriptor left, DataModelDescriptor right) => !left.Equals(right);
	}
}