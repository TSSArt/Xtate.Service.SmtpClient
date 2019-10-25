namespace TSSArt.StateMachine
{
	public sealed class DataList : ValidatedReadOnlyList<DataList, IData>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}