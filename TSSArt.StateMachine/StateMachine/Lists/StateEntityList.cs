namespace TSSArt.StateMachine
{
	public sealed class StateEntityList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}