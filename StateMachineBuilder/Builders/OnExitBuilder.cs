using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class OnExitBuilder : IOnExitBuilder
	{
		private readonly List<IExecutableEntity> _actions = new List<IExecutableEntity>();

		public IOnExit Build() => new OnExit { Action = ExecutableEntityList.Create(_actions) };

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			_actions.Add(action);
		}
	}
}