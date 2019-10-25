namespace TSSArt.StateMachine
{
	public sealed class EventDescriptorList : ValidatedReadOnlyList<EventDescriptorList, IEventDescriptor>
	{
		protected override Options GetOptions() => Options.NonEmpty;
	}
}