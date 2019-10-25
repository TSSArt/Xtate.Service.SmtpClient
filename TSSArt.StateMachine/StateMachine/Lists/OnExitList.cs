namespace TSSArt.StateMachine
{
	public sealed class OnExitList : ValidatedReadOnlyList<OnExitList, IOnExit>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}