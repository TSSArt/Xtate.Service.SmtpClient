namespace TSSArt.StateMachine
{
	public class ElseBuilder : IElseBuilder
	{
		public IElse Build() => new Else();
	}
}