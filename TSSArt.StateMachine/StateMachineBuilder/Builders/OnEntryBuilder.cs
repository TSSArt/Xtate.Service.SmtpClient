using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class OnEntryBuilder : IOnEntryBuilder
	{
		private readonly List<IExecutableEntity> _actions = new List<IExecutableEntity>();

		public IOnEntry Build() => new OnEntry { Action = ExecutableEntityList.Create(_actions) };

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			_actions.Add(action);
		}
	}
}