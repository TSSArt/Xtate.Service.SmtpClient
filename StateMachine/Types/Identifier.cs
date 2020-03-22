using System;

namespace TSSArt.StateMachine
{
	public sealed class Identifier : IIdentifier, IEquatable<IIdentifier>, IAncestorProvider
	{
		private readonly string _val;

		private Identifier(string val)
		{
			if (string.IsNullOrEmpty(val)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(val));

			_val = val;

			for (var i = 0; i < val.Length; i ++)
			{
				if (char.IsWhiteSpace(val, i))
				{
					throw new ArgumentException(Resources.Exception_IdentifierCannotContainsWhitespace, nameof(val));
				}
			}
		}

		object IAncestorProvider.Ancestor => _val;

		public bool Equals(IIdentifier other) => Equals((object) other);

		public static explicit operator Identifier(string val) => new Identifier(val);

		public static Identifier FromString(string val) => new Identifier(val);

		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Identifier other && _val.Equals(other._val, StringComparison.Ordinal);

		public override int GetHashCode() => _val.GetHashCode();
	}
}