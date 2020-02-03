namespace TSSArt.StateMachine
{
	public sealed class IdentifierList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NonEmpty;
	}
}