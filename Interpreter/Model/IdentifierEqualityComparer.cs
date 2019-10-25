using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class IdentifierEqualityComparer : IEqualityComparer<IIdentifier>
	{
		public static readonly IEqualityComparer<IIdentifier> Instance = new IdentifierEqualityComparer();

		public bool Equals(IIdentifier x, IIdentifier y) => object.Equals(x.Base<IIdentifier>(), y.Base<IIdentifier>());

		public int GetHashCode(IIdentifier obj) => obj.Base<IIdentifier>().GetHashCode();
	}
}