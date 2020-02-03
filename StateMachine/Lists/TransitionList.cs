namespace TSSArt.StateMachine
{
	public sealed class TransitionList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}