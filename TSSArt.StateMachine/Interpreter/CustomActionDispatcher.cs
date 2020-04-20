using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	internal sealed class CustomActionDispatcher : ICustomAction, ICustomActionDispatcher, ICustomActionContext
	{
		private readonly CustomAction _customAction;

		private ICustomActionExecutor?                       _executor;
		private ImmutableArray<ILocationEvaluator>           _locationEvaluators;
		private ImmutableArray<ILocationExpression>.Builder? _locations;
		private ImmutableArray<IObjectEvaluator>             _objectEvaluators;
		private ImmutableArray<IValueExpression>.Builder?    _values;

		public CustomActionDispatcher(in CustomAction customAction)
		{
			_customAction = customAction;

			Infrastructure.Assert(customAction.Xml != null);
		}

	#region Interface ICustomAction

		public ImmutableArray<ILocationExpression> Locations { get; private set; }

		public ImmutableArray<IValueExpression> Values { get; private set; }

		string? ICustomAction.Xml => _customAction.Xml;

	#endregion

	#region Interface ICustomActionContext

		ILocationAssigner ICustomActionContext.RegisterLocationExpression(string expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			if (_executor != null)
			{
				throw new StateMachineInfrastructureException(Resources.Exception_Registration_should_no_occur_after_initialization);
			}

			_locations ??= ImmutableArray.CreateBuilder<ILocationExpression>();

			var locationAssigner = new LocationAssigner(this, _locations.Count, expression);
			_locations.Add(locationAssigner);

			return locationAssigner;
		}

		IExpressionEvaluator ICustomActionContext.RegisterValueExpression(string expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			if (_executor != null)
			{
				throw new StateMachineInfrastructureException(Resources.Exception_Registration_should_no_occur_after_initialization);
			}

			_values ??= ImmutableArray.CreateBuilder<IValueExpression>();

			var expressionEvaluator = new ExpressionEvaluator(this, _values.Count, expression);
			_values.Add(expressionEvaluator);

			return expressionEvaluator;
		}

		string ICustomActionContext.Xml => _customAction.Xml!;

	#endregion

	#region Interface ICustomActionDispatcher

		public void SetEvaluators(ImmutableArray<ILocationEvaluator> locationEvaluators, ImmutableArray<IObjectEvaluator> objectEvaluators)
		{
			_locationEvaluators = locationEvaluators;
			_objectEvaluators = objectEvaluators;
		}

		public ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			Infrastructure.Assert(_executor != null);

			return _executor.Execute(executionContext, token);
		}

	#endregion

		public void SetupExecutor(ImmutableArray<ICustomActionFactory> customActionProviders)
		{
			var executor = GetExecutor(customActionProviders);

			Locations = _locations?.ToImmutable() ?? default;
			Values = _values?.ToImmutable() ?? default;

			_executor = executor;
		}

		private ICustomActionExecutor GetExecutor(ImmutableArray<ICustomActionFactory> customActionFactories)
		{
			if (!customActionFactories.IsDefaultOrEmpty)
			{
				using var stringReader = new StringReader(_customAction.Xml);
				using var xmlReader = XmlReader.Create(stringReader);

				xmlReader.MoveToContent();

				var namespaceUri = xmlReader.NamespaceURI;
				var name = xmlReader.LocalName;

				foreach (var factory in customActionFactories)
				{
					if (factory.CanHandle(namespaceUri, name))
					{
						return factory.CreateExecutor(this);
					}
				}
			}

			return CustomActionBase.NoExecutorInstance;
		}

		private class LocationAssigner : ILocationAssigner, ILocationExpression
		{
			private readonly CustomActionDispatcher _dispatcher;
			private readonly int                    _index;

			public LocationAssigner(CustomActionDispatcher dispatcher, int index, string expression)
			{
				_dispatcher = dispatcher;
				_index = index;
				Expression = expression;
			}

		#region Interface ILocationAssigner

			public ValueTask Assign(IExecutionContext executionContext, DataModelValue value, CancellationToken token)
			{
				if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

				Infrastructure.Assert(!_dispatcher._locationEvaluators.IsDefault);

				var locationEvaluator = _dispatcher._locationEvaluators[_index];

				return locationEvaluator.SetValue(value, executionContext, token);
			}

		#endregion

		#region Interface ILocationExpression

			public string Expression { get; }

		#endregion
		}

		private class ExpressionEvaluator : IExpressionEvaluator, IValueExpression
		{
			private readonly CustomActionDispatcher _dispatcher;
			private readonly int                    _index;

			public ExpressionEvaluator(CustomActionDispatcher dispatcher, int index, string expression)
			{
				_dispatcher = dispatcher;
				_index = index;
				Expression = expression;
			}

		#region Interface IExpressionEvaluator

			public ValueTask<DataModelValue> Evaluate(IExecutionContext executionContext, CancellationToken token)
			{
				if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

				Infrastructure.Assert(!_dispatcher._objectEvaluators.IsDefault);

				var objectEvaluator = _dispatcher._objectEvaluators[_index];

				return EvaluateValueLocal();

				async ValueTask<DataModelValue> EvaluateValueLocal()
				{
					var obj = await objectEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);

					return DataModelValue.FromObject(obj);
				}
			}

		#endregion

		#region Interface IValueExpression

			public string Expression { get; }

		#endregion
		}
	}
}