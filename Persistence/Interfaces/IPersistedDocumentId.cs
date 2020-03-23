using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	internal interface IPersistedDocumentId
	{
		int DocumentId { get; }
	}
}