using System;

namespace TSSArt.StateMachine
{
	internal sealed class RuntimeIdentifier : IIdentifier, IEquatable<IIdentifier>, IAncestorProvider
	{
		private string? _val;

	#region Interface IAncestorProvider

		object IAncestorProvider.Ancestor => _val ??= IdGenerator.NewUniqueStateId();

	#endregion

	#region Interface IEquatable<IIdentifier>

		public bool Equals(IIdentifier other) => ReferenceEquals(this, other);

	#endregion
	}
}