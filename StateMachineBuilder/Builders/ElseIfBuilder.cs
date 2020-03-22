using System;

namespace TSSArt.StateMachine
{
	public class ElseIfBuilder : BuilderBase, IElseIfBuilder
	{
		private IConditionExpression? _condition;

		public ElseIfBuilder(IErrorProcessor errorProcessor, object? ancestor) : base(errorProcessor, ancestor)
		{ }

		public IElseIf Build() => new ElseIfEntity { Ancestor = Ancestor, Condition = _condition };

		public void SetCondition(IConditionExpression condition) => _condition = condition ?? throw new ArgumentNullException(nameof(condition));
	}
}