namespace TSSArt.StateMachine
{
	public sealed class InvokeList : ValidatedReadOnlyList<InvokeList, IInvoke>
	{
		protected override Options GetOptions() => Options.NullIfEmpty;
	}
}