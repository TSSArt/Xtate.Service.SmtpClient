using System;
using System.Diagnostics.CodeAnalysis;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public sealed class Identifier : IIdentifier, IEquatable<IIdentifier>, IAncestorProvider
	{
		private readonly string _val;

		private Identifier(string val) => _val = val;

	#region Interface IAncestorProvider

		object IAncestorProvider.Ancestor => _val;

	#endregion

	#region Interface IEquatable<IIdentifier>

		public bool Equals(IIdentifier other) => Equals((object) other);

	#endregion

		public static explicit operator Identifier(string val) => FromString(val);

		public static Identifier FromString(string val)
		{
			if (string.IsNullOrEmpty(val)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(val));

			foreach (var ch in val)
			{
				if (char.IsWhiteSpace(ch))
				{
					throw new ArgumentException(Resources.Exception_IdentifierCannotContainsWhitespace, nameof(val));
				}
			}

			return new Identifier(val);
		}

		public static bool TryCreate(string? val, [NotNullWhen(true)] [MaybeNullWhen(false)]
									 out Identifier? identifier)
		{
			if (string.IsNullOrEmpty(val))
			{
				identifier = null;

				return false;
			}

			foreach (var ch in val)
			{
				if (char.IsWhiteSpace(ch))
				{
					identifier = null;

					return false;
				}
			}

			identifier = new Identifier(val);

			return true;
		}

		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Identifier other && _val.Equals(other._val, StringComparison.Ordinal);

		public override int GetHashCode() => _val.GetHashCode();
	}
}