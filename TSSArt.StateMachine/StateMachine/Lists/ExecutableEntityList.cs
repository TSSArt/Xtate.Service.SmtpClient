namespace TSSArt.StateMachine
{
	public sealed class ExecutableEntityList : ValidatedReadOnlyList<ExecutableEntityList, IExecutableEntity>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}