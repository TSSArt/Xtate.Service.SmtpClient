namespace TSSArt.StateMachine
{
	public sealed class ExecutableEntityList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}