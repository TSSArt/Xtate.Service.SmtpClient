using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class OnExitBuilder : IOnExitBuilder
	{
		private ImmutableArray<IExecutableEntity>.Builder _actions;

		public IOnExit Build() => new OnExit { Action = _actions?.ToImmutable() ?? default };

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			(_actions ??= ImmutableArray.CreateBuilder<IExecutableEntity>()).Add(action);
		}
	}
}