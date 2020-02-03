namespace TSSArt.StateMachine
{
	public sealed class DataList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}