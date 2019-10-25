using System;

namespace TSSArt.StateMachine
{
	public class InitialBuilder : IInitialBuilder
	{
		private ITransition _transition;

		public IInitial Build()
		{
			if (_transition == null)
			{
				throw new InvalidOperationException(message: "Transition must be present in Initial element");
			}

			return new Initial { Transition = _transition };
		}

		public void SetTransition(ITransition transition) => _transition = transition ?? throw new ArgumentNullException(nameof(transition));
	}
}