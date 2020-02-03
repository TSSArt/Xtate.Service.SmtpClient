using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
			var currentActions = ImmutableArray.CreateBuilder<IExecEvaluator>();

			Branches = new List<(IBooleanEvaluator Condition, ImmutableArray<IExecEvaluator> Actions)>();

			foreach (var op in @if.Action)
			{
				switch (op)
				{
					case IElseIf elseIf:
						Branches.Add((currentCondition, currentActions.ToImmutable()));
						currentCondition = elseIf.Condition.As<IBooleanEvaluator>();
						currentActions.Clear();
						break;

					case IElse _:
						Branches.Add((currentCondition, currentActions.ToImmutable()));
						currentCondition = null;
						currentActions.Clear();
						break;

					default:
						currentActions.Add(op.As<IExecEvaluator>());
						break;
				}
			}

			Branches.Add((currentCondition, currentActions.ToImmutable()));
		}

		public List<(IBooleanEvaluator Condition, ImmutableArray<IExecEvaluator> Actions)> Branches { get; }

		object IAncestorProvider.Ancestor => _if.Ancestor;

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

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

		public ImmutableArray<IExecutableEntity> Action    => _if.Action;
		public IConditionExpression             Condition => _if.Condition;
	}
}