namespace TSSArt.StateMachine
{
	public sealed class HistoryList : ValidatedReadOnlyList<HistoryList, IHistory>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}