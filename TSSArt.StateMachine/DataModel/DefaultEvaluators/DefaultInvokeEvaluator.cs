using System;
using System.Collections.Generic;
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
			IdLocationEvaluator = invoke.IdLocation.As<ILocationEvaluator>();
			NameEvaluatorList = invoke.NameList.AsListOf<ILocationEvaluator>();
			ParameterList = invoke.Parameters.AsListOf<DefaultParam>();
		}

		public IObjectEvaluator                  ContentExpressionEvaluator { get; }
		public ILocationEvaluator                IdLocationEvaluator        { get; }
		public IReadOnlyList<ILocationEvaluator> NameEvaluatorList          { get; }
		public IReadOnlyList<DefaultParam>       ParameterList              { get; }
		public IStringEvaluator                  SourceExpressionEvaluator  { get; }
		public IStringEvaluator                  TypeExpressionEvaluator    { get; }

		object IAncestorProvider.Ancestor => _invoke.Ancestor;

		public virtual ValueTask Cancel(string invokeId, IExecutionContext executionContext, CancellationToken token) => executionContext.CancelInvoke(invokeId, token);

		public Uri                                Type             => _invoke.Type;
		public IValueExpression                   TypeExpression   => _invoke.TypeExpression;
		public Uri                                Source           => _invoke.Source;
		public IValueExpression                   SourceExpression => _invoke.SourceExpression;
		public string                             Id               => _invoke.Id;
		public ILocationExpression                IdLocation       => _invoke.IdLocation;
		public IReadOnlyList<ILocationExpression> NameList         => _invoke.NameList;
		public bool                               AutoForward      => _invoke.AutoForward;
		public IReadOnlyList<IParam>              Parameters       => _invoke.Parameters;
		public IFinalize                          Finalize         => _invoke.Finalize;
		public IContent                           Content          => _invoke.Content;

		public virtual async ValueTask<string> Start(string stateId, IExecutionContext executionContext, CancellationToken token)
		{
			var invokeId = _invoke.Id ?? IdGenerator.NewInvokeId(stateId);

			var type = TypeExpressionEvaluator != null ? ToUri(await TypeExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _invoke.Type;
			var source = SourceExpressionEvaluator != null ? ToUri(await SourceExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _invoke.Source;

			var data = await Converter.GetData(_invoke.Content?.Value, ContentExpressionEvaluator, NameEvaluatorList, ParameterList, executionContext, token).ConfigureAwait(false);

			await executionContext.StartInvoke(invokeId, type, source, data, token);

			IdLocationEvaluator?.SetValue(new DefaultObject(invokeId), executionContext);

			return invokeId;
		}

		private static Uri ToUri(string uri) => new Uri(uri, UriKind.RelativeOrAbsolute);
	}
}