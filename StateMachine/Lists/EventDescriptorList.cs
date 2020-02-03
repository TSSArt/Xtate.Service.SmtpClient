namespace TSSArt.StateMachine
{
	public sealed class EventDescriptorList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NonEmpty;
	}
}