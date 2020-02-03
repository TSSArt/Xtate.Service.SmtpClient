namespace TSSArt.StateMachine
{
	public sealed class OnEntryList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}