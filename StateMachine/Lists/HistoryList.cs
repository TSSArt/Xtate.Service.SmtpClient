namespace TSSArt.StateMachine
{
	public sealed class HistoryList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}