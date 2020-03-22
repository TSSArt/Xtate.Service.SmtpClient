namespace TSSArt.StateMachine
{
	public class ElseBuilder : BuilderBase, IElseBuilder
	{
		public ElseBuilder(IErrorProcessor errorProcessor, object? ancestor) : base(errorProcessor, ancestor)
		{ }

		public IElse Build() => new ElseEntity { Ancestor = Ancestor };
	}
}