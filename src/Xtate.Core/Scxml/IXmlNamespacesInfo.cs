using System.Collections.Immutable;

namespace Xtate
{
	public interface IXmlNamespacesInfo
	{
		ImmutableArray<PrefixNamespace> Namespaces { get; }
	}
}