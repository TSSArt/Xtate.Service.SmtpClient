using System;
using System.Threading;
using System.Threading.Tasks;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public class DefaultAssignEvaluator : IAssign, IExecEvaluator, IAncestorProvider
	{
		private readonly AssignEntity _assign;

		public DefaultAssignEvaluator(in AssignEntity assign)
		{
			_assign = assign;

			Infrastructure.Assert(assign.Location != null);

			LocationEvaluator = assign.Location.As<ILocationEvaluator>();
			ExpressionEvaluator = assign.Expression?.As<IObjectEvaluator>();
		}

		public ILocationEvaluator LocationEvaluator   { get; }
		public IObjectEvaluator?  ExpressionEvaluator { get; }

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _assign.Ancestor;

	#endregion

	#region Interface IAssign

		public ILocationExpression Location      => _assign.Location!;
		public IValueExpression?   Expression    => _assign.Expression;
		public string?             InlineContent => _assign.InlineContent;

	#endregion

	#region Interface IExecEvaluator

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			IObject value;

			if (ExpressionEvaluator != null)
			{
				value = await ExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);
			}
			else if (InlineContent != null)
			{
				value = new DefaultObject(InlineContent);
			}
			else
			{
				value = DefaultObject.Null;
			}

			await LocationEvaluator.SetValue(value, executionContext, token).ConfigureAwait(false);
		}

	#endregion
	}
}