namespace TSSArt.StateMachine
{
	public sealed class InvokeList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}