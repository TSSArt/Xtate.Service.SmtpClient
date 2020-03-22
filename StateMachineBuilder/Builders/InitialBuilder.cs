using System;

namespace TSSArt.StateMachine
{
	public class InitialBuilder : BuilderBase, IInitialBuilder
	{
		private ITransition? _transition;

		public InitialBuilder(IErrorProcessor errorProcessor, object? ancestor) : base(errorProcessor, ancestor)
		{ }

		public IInitial Build() => new InitialEntity { Ancestor = Ancestor, Transition = _transition };

		public void SetTransition(ITransition transition) => _transition = transition ?? throw new ArgumentNullException(nameof(transition));
	}
}