using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultAssignEvaluator : IAssign, IExecEvaluator, IAncestorProvider
	{
		private readonly Assign _assign;

		public DefaultAssignEvaluator(in Assign assign)
		{
			_assign = assign;

			LocationEvaluator = assign.Location.As<ILocationEvaluator>();
			ExpressionEvaluator = assign.Expression.As<IObjectEvaluator>();
		}

		public ILocationEvaluator LocationEvaluator   { get; }
		public IObjectEvaluator   ExpressionEvaluator { get; }

		object IAncestorProvider.Ancestor => _assign.Ancestor;

		public ILocationExpression Location      => _assign.Location;
		public IValueExpression    Expression    => _assign.Expression;
		public string              InlineContent => _assign.InlineContent;

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			IObject value = DataModelValue.Undefined;

			if (ExpressionEvaluator != null)
			{
				value = await ExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);
			}
			else if (InlineContent != null)
			{
				value = DataModelValue.FromInlineContent(InlineContent);
			}

			LocationEvaluator.SetValue(value, executionContext);
		}
	}
}