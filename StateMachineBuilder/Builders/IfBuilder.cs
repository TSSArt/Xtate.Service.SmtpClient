using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class IfBuilder : IIfBuilder
	{
		private readonly List<IExecutableEntity> _actions = new List<IExecutableEntity>();
		private          IConditionExpression    _condition;

		public IIf Build()
		{
			if (_condition == null)
			{
				throw new InvalidOperationException(message: "Condition property required for If element");
			}

			return new If { Condition = _condition, Action = _actions };
		}

		public void SetCondition(IConditionExpression condition) => _condition = condition ?? throw new ArgumentNullException(nameof(condition));

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			_actions.Add(action);
		}
	}
}