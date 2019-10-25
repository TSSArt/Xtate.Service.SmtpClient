namespace TSSArt.StateMachine
{
	public sealed class IdentifierList : ValidatedReadOnlyList<IdentifierList, IIdentifier>
	{
		protected override Options GetOptions() => Options.NonEmpty;
	}
}