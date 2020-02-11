using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class OnEntryBuilder : IOnEntryBuilder
	{
		private ImmutableArray<IExecutableEntity>.Builder _actions;

		public IOnEntry Build() => new OnEntry { Action = _actions?.ToImmutable() ?? default };

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			(_actions ??= ImmutableArray.CreateBuilder<IExecutableEntity>()).Add(action);
		}
	}
}