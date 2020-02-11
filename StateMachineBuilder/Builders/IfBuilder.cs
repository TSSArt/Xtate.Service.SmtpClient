using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class IfBuilder : IIfBuilder
	{
		private ImmutableArray<IExecutableEntity>.Builder _actions;
		private IConditionExpression                      _condition;

		public IIf Build()
		{
			if (_condition == null)
			{
				throw new InvalidOperationException(message: "Condition property required for If element");
			}

			return new If { Condition = _condition, Action = _actions?.ToImmutable() ?? default };
		}

		public void SetCondition(IConditionExpression condition) => _condition = condition ?? throw new ArgumentNullException(nameof(condition));

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			(_actions ??= ImmutableArray.CreateBuilder<IExecutableEntity>()).Add(action);
		}
	}
}