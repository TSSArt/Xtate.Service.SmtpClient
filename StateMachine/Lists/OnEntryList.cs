namespace TSSArt.StateMachine
{
	public sealed class OnEntryList : ValidatedReadOnlyList<OnEntryList, IOnEntry>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}