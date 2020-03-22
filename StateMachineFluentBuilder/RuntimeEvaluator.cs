using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public delegate DataModelValue Evaluator(IExecutionContext executionContext);

	public delegate ValueTask<DataModelValue> EvaluatorTask(IExecutionContext executionContext);

	public delegate ValueTask<DataModelValue> EvaluatorCancellableTask(IExecutionContext executionContext, CancellationToken token);

	public class RuntimeEvaluator : IValueExpression, IObjectEvaluator
	{
		private readonly object _evaluator;

		public RuntimeEvaluator(Evaluator evaluator) => _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

		public RuntimeEvaluator(EvaluatorTask task) => _evaluator = task ?? throw new ArgumentNullException(nameof(task));

		public RuntimeEvaluator(EvaluatorCancellableTask task) => _evaluator = task ?? throw new ArgumentNullException(nameof(task));

		public async ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token) =>
				_evaluator switch
				{
						Evaluator evaluator => new DefaultObject(evaluator(executionContext)),
						EvaluatorTask task => new DefaultObject(await task(executionContext).ConfigureAwait(false)),
						EvaluatorCancellableTask task => new DefaultObject(await task(executionContext, token).ConfigureAwait(false)),
						_ => Infrastructure.UnexpectedValue<IObject>()
				};

		public string? Expression => null;
	}
}