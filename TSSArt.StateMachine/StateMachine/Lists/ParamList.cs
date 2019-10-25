namespace TSSArt.StateMachine
{
	public sealed class ParamList : ValidatedReadOnlyList<ParamList, IParam>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}