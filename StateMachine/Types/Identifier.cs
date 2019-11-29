using System;

namespace TSSArt.StateMachine
{
	public sealed class Identifier : IIdentifier
	{
		private readonly string _val;

		private Identifier(string val)
		{
			_val = val ?? throw new ArgumentNullException(nameof(val));

			if (val.Length == 0)
			{
				throw new ArgumentException(message: "Identifier cannot be empty", nameof(val));
			}

			for (var i = 0; i < val.Length; i ++)
			{
				if (char.IsWhiteSpace(val, i))
				{
					throw new ArgumentException(message: "Identifier cannot contains whitespace", nameof(val));
				}
			}
		}

		public static explicit operator Identifier(string val) => new Identifier(val);

		public static Identifier FromString(string val) => new Identifier(val);

		public override string ToString() => _val;

		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Identifier other && _val.Equals(other._val, StringComparison.Ordinal);

		public override int GetHashCode() => _val.GetHashCode();
	}
}