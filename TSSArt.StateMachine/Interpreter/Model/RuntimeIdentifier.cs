using System;

namespace TSSArt.StateMachine
{
	internal sealed class RuntimeIdentifier : IIdentifier, IEquatable<IIdentifier>, IAncestorProvider
	{
		private string? _val;

		object IAncestorProvider.Ancestor => _val ??= IdGenerator.NewUniqueStateId();

		public bool Equals(IIdentifier other) => ReferenceEquals(this, other);
	}
}