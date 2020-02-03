using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultInvokeEvaluator : IInvoke, IStartInvokeEvaluator, ICancelInvokeEvaluator, IAncestorProvider
	{
		private readonly Invoke _invoke;

		public DefaultInvokeEvaluator(in Invoke invoke)
		{
			_invoke = invoke;

			TypeExpressionEvaluator = invoke.TypeExpression.As<IStringEvaluator>();
			SourceExpressionEvaluator = invoke.SourceExpression.As<IStringEvaluator>();
			ContentExpressionEvaluator = invoke.Content?.Expression.As<IObjectEvaluator>();
			ContentBodyEvaluator = invoke.Content?.Body.As<IValueEvaluator>();
			IdLocationEvaluator = invoke.IdLocation.As<ILocationEvaluator>();
			NameEvaluatorList = invoke.NameList.As<IEntity>().AsListOf<ILocationEvaluator>();
			ParameterList = invoke.Parameters.As<IEntity>().AsListOf<DefaultParam>();
		}

		public IObjectEvaluator                  ContentExpressionEvaluator { get; }
		public IValueEvaluator                   ContentBodyEvaluator       { get; }
		public ILocationEvaluator                IdLocationEvaluator        { get; }
		public ImmutableArray<ILocationEvaluator> NameEvaluatorList          { get; }
		public ImmutableArray<DefaultParam>       ParameterList              { get; }
		public IStringEvaluator                  SourceExpressionEvaluator  { get; }
		public IStringEvaluator                  TypeExpressionEvaluator    { get; }

		object IAncestorProvider.Ancestor => _invoke.Ancestor;

		public virtual ValueTask Cancel(string invokeId, IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));
			if (string.IsNullOrEmpty(invokeId)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(invokeId));

			return executionContext.CancelInvoke(invokeId, token);
		}

		public Uri                                Type             => _invoke.Type;
		public IValueExpression                   TypeExpression   => _invoke.TypeExpression;
		public Uri                                Source           => _invoke.Source;
		public IValueExpression                   SourceExpression => _invoke.SourceExpression;
		public string                             Id               => _invoke.Id;
		public ILocationExpression                IdLocation       => _invoke.IdLocation;
		public ImmutableArray<ILocationExpression> NameList         => _invoke.NameList;
		public bool                               AutoForward      => _invoke.AutoForward;
		public ImmutableArray<IParam>              Parameters       => _invoke.Parameters;
		public IFinalize                          Finalize         => _invoke.Finalize;
		public IContent                           Content          => _invoke.Content;

		public virtual async ValueTask<(string InvokeId, string InvokeUniqueId)> Start(string stateId, IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));
			if (string.IsNullOrEmpty(stateId)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(stateId));

			var invokeId = _invoke.Id ?? IdGenerator.NewInvokeId(stateId);
			var invokeUniqueId = _invoke.Id != null ? IdGenerator.NewInvokeUniqueId() : invokeId;

			IdLocationEvaluator?.SetValue(new DefaultObject(invokeId), executionContext);

			var type = TypeExpressionEvaluator != null ? ToUri(await TypeExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _invoke.Type;
			var source = SourceExpressionEvaluator != null ? ToUri(await SourceExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _invoke.Source;

			var rawContent = ContentBodyEvaluator is IStringEvaluator rawContentEvaluator ? await rawContentEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false) : null;
			var content = await DataConverter.GetContent(ContentBodyEvaluator, ContentExpressionEvaluator, executionContext, token).ConfigureAwait(false);
			var parameters = await DataConverter.GetParameters(NameEvaluatorList, ParameterList, executionContext, token).ConfigureAwait(false);

			var invokeData = new InvokeData
							 {
									 InvokeId = invokeId,
									 InvokeUniqueId = invokeUniqueId,
									 Type = type,
									 Source = source,
									 RawContent = rawContent,
									 Content = content,
									 Parameters = parameters
							 };

			await executionContext.StartInvoke(invokeData, token).ConfigureAwait(false);

			return (invokeId, invokeUniqueId);
		}

		private static Uri ToUri(string uri) => new Uri(uri, UriKind.RelativeOrAbsolute);
	}
}