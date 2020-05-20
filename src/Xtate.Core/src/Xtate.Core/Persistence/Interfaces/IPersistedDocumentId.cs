using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	internal interface IPersistedDocumentId
	{
		int DocumentId { get; }
	}
}