namespace TSSArt.StateMachine
{
	public sealed class TransitionList : ValidatedReadOnlyList<TransitionList, ITransition>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}