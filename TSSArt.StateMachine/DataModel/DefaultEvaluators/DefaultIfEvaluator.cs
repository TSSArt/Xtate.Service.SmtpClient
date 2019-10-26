using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultIfEvaluator : IIf, IExecEvaluator, IAncestorProvider
	{
		private readonly If _if;

		public DefaultIfEvaluator(in If @if)
		{
			_if = @if;

			var currentCondition = @if.Condition.As<IBooleanEvaluator>();
			var currentActions = new List<IExecEvaluator>();

			Branches = new List<(IBooleanEvaluator Condition, IReadOnlyList<IExecEvaluator> Actions)>();

			foreach (var op in @if.Action)
			{
				switch (op)
				{
					case IElseIf elseIf:
						Branches.Add((currentCondition, currentActions.AsReadOnly()));
						currentCondition = elseIf.Condition.As<IBooleanEvaluator>();
						currentActions = new List<IExecEvaluator>();
						break;

					case IElse _:
						Branches.Add((currentCondition, currentActions.AsReadOnly()));
						currentCondition = null;
						currentActions = new List<IExecEvaluator>();
						break;

					default:
						currentActions.Add(op.As<IExecEvaluator>());
						break;
				}
			}

			Branches.Add((currentCondition, currentActions));
		}

		public List<(IBooleanEvaluator Condition, IReadOnlyList<IExecEvaluator> Actions)> Branches { get; }

		object IAncestorProvider.Ancestor => _if.Ancestor;

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			foreach (var (condition, actions) in Branches)
			{
				if (condition == null || await condition.EvaluateBoolean(executionContext, token).ConfigureAwait(false))
				{
					foreach (var action in actions)
					{
						await action.Execute(executionContext, token).ConfigureAwait(false);
					}

					return;
				}
			}
		}

		public IReadOnlyList<IExecutableEntity> Action    => _if.Action;
		public IConditionExpression             Condition => _if.Condition;
	}
}