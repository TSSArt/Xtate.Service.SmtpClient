namespace TSSArt.StateMachine
{
	public sealed class LocationExpressionList : ValidatedArrayBuilder<>
	{
		protected override Options GetOptions() => Options.NonEmpty;
	}
}