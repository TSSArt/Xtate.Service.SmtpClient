namespace TSSArt.StateMachine
{
	public sealed class LocationExpressionList : ValidatedReadOnlyList<LocationExpressionList, ILocationExpression>
	{
		protected override Options GetOptions() => Options.NonEmpty;
	}
}