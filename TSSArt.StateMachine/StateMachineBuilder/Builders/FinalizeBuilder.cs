using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class FinalizeBuilder : IFinalizeBuilder
	{
		private readonly List<IExecutableEntity> _actions = new List<IExecutableEntity>();

		public IFinalize Build() => new Finalize { Action = ExecutableEntityList.Create(_actions) };

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			if (action is IRaise)
			{
				throw new InvalidOperationException("Raise can't be used in Finalize element");
			}

			if (action is ISend)
			{
				throw new InvalidOperationException("Send can't be used in Finalize element");
			}

			_actions.Add(action);
		}
	}
}