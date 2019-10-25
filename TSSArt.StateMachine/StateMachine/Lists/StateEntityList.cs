namespace TSSArt.StateMachine
{
	public sealed class StateEntityList : ValidatedReadOnlyList<StateEntityList, IStateEntity>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}