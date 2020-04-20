using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class CustomActionBase : ICustomActionExecutor
	{
		internal static readonly ICustomActionExecutor NoExecutorInstance = new CustomActionBase(null!);

		private readonly ICustomActionContext         _customActionContext;
		private          Dictionary<string, object?>? _arguments;
		private          ILocationAssigner?           _resultLocationAssigner;

		protected CustomActionBase(ICustomActionContext customActionContext) => _customActionContext = customActionContext ?? throw new ArgumentNullException(nameof(customActionContext));

	#region Interface ICustomActionExecutor

		ValueTask ICustomActionExecutor.Execute(IExecutionContext context, CancellationToken token)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			return Execute(context, token);
		}

	#endregion

		protected void RegisterArgument(string key, string expression, string? constant = null)
		{
			_arguments ??= new Dictionary<string, object?>();
			_arguments.Add(key, expression != null ? (object) _customActionContext.RegisterValueExpression(expression) : constant);
		}

		protected void RegisterResultLocation(string expression)
		{
			if (expression != null)
			{
				_resultLocationAssigner = _customActionContext.RegisterLocationExpression(expression);
			}
		}

		protected virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			var arguments = ImmutableDictionary<string, DataModelValue>.Empty;

			if (_arguments != null)
			{
				var builder = ImmutableDictionary.CreateBuilder<string, DataModelValue>();

				foreach (var pair in _arguments)
				{
					if (pair.Value is IExpressionEvaluator expressionEvaluator)
					{
						builder.Add(pair.Key, await expressionEvaluator.Evaluate(executionContext, token).ConfigureAwait(false));
					}
					else if (pair.Value is string str)
					{
						builder.Add(pair.Key, str);
					}
					else
					{
						builder.Add(pair.Key, DataModelValue.Undefined);
					}
				}

				arguments = builder.ToImmutable();
			}

			var result = Evaluate(arguments);

			if (_resultLocationAssigner != null)
			{
				await _resultLocationAssigner.Assign(executionContext, result, token).ConfigureAwait(false);
			}
		}

		protected virtual DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments) => throw new NotSupportedException(Resources.Exception_CustomActionDoesNotSupported);
	}
}