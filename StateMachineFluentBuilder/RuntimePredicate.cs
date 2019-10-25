using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public delegate ValueTask<bool> PredicateTask(IExecutionContext executionContext, CancellationToken token);

	public delegate bool Predicate(IExecutionContext executionContext);

	public class RuntimePredicate : IExecutableEntity, IBooleanEvaluator
	{
		private readonly Predicate     _predicate;
		private readonly PredicateTask _predicateTask;

		public RuntimePredicate(PredicateTask predicateTask) => _predicateTask = predicateTask ?? throw new ArgumentNullException(nameof(predicateTask));

		public RuntimePredicate(Predicate predicate) => _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

		public ValueTask<bool> EvaluateBoolean(IExecutionContext executionContext, CancellationToken token) =>
				_predicateTask?.Invoke(executionContext, token) ?? new ValueTask<bool>(_predicate(executionContext));
	}
}