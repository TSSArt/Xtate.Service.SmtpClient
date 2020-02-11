using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal sealed class NoneConditionExpressionEvaluator : IConditionExpression, IBooleanEvaluator, IAncestorProvider, IDebugEntityId
	{
		private readonly ConditionExpression _conditionExpression;
		private readonly IIdentifier         _inState;

		public NoneConditionExpressionEvaluator(in ConditionExpression conditionExpression, IIdentifier inState)
		{
			_conditionExpression = conditionExpression;
			_inState = inState;
		}

		object IAncestorProvider.Ancestor => _conditionExpression.Ancestor;

		ValueTask<bool> IBooleanEvaluator.EvaluateBoolean(IExecutionContext executionContext, CancellationToken token) => new ValueTask<bool>(executionContext.InState(_inState));

		public string Expression => _conditionExpression.Expression;

		FormattableString IDebugEntityId.EntityId => $"{_inState}";
	}
}