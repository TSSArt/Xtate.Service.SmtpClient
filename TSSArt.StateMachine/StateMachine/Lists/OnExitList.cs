namespace TSSArt.StateMachine
{
	public sealed class OnExitList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}