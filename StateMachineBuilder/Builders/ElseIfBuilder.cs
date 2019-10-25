using System;

namespace TSSArt.StateMachine
{
	public class ElseIfBuilder : IElseIfBuilder
	{
		private IConditionExpression _condition;

		public IElseIf Build()
		{
			if (_condition == null)
			{
				throw new InvalidOperationException(message: "Condition property required for ElseIf element");
			}

			return new ElseIf { Condition = _condition };
		}

		public void SetCondition(IConditionExpression condition) => _condition = condition ?? throw new ArgumentNullException(nameof(condition));
	}
}