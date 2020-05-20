using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	internal interface IPersistedDocumentId
	{
		int DocumentId { get; }
	}
}