using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultSendEvaluator : IExecEvaluator, ISend, IAncestorProvider, IDebugEntityId
	{
		private readonly Send _send;

		public DefaultSendEvaluator(in Send send)
		{
			_send = send;
			EventExpressionEvaluator = send.EventExpression.As<IStringEvaluator>();
			TypeExpressionEvaluator = send.TypeExpression.As<IStringEvaluator>();
			TargetExpressionEvaluator = send.TargetExpression.As<IStringEvaluator>();
			DelayExpressionEvaluator = send.DelayExpression.As<IIntegerEvaluator>();
			ContentExpressionEvaluator = send.Content?.Expression.As<IObjectEvaluator>();
			ContentBodyEvaluator = send.Content?.Body.As<IValueEvaluator>();
			IdLocationEvaluator = send.IdLocation.As<ILocationEvaluator>();
			NameEvaluatorList = send.NameList.AsListOf<ILocationEvaluator>();
			ParameterList = send.Parameters.AsListOf<DefaultParam>();
		}

		public IObjectEvaluator                  ContentExpressionEvaluator { get; }
		public IValueEvaluator                   ContentBodyEvaluator       { get; }
		public IIntegerEvaluator                 DelayExpressionEvaluator   { get; }
		public IStringEvaluator                  EventExpressionEvaluator   { get; }
		public ILocationEvaluator                IdLocationEvaluator        { get; }
		public IStringEvaluator                  TargetExpressionEvaluator  { get; }
		public IStringEvaluator                  TypeExpressionEvaluator    { get; }
		public IReadOnlyList<ILocationEvaluator> NameEvaluatorList          { get; }
		public IReadOnlyList<DefaultParam>       ParameterList              { get; }

		object IAncestorProvider.Ancestor => _send.Ancestor;

		public FormattableString EntityId => $"{Id}";

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			var sendId = _send.Id ?? IdGenerator.NewSendId();
			var name = EventExpressionEvaluator != null ? await EventExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false) : _send.Event;
			var data = await DataConverter.GetData(ContentBodyEvaluator, ContentExpressionEvaluator, NameEvaluatorList, ParameterList, executionContext, token).ConfigureAwait(false);
			var type = TypeExpressionEvaluator != null ? ToUri(await TypeExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _send.Type;
			var target = TargetExpressionEvaluator != null ? ToUri(await TargetExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _send.Target;
			var delayMs = DelayExpressionEvaluator != null ? await DelayExpressionEvaluator.EvaluateInteger(executionContext, token).ConfigureAwait(false) : _send.DelayMs ?? 0;

			var eventObject = new Event(name)
							  {
									  SendId = sendId,
									  Type = type,
									  Target = target,
									  DelayMs = delayMs,
									  Data = data
							  };

			if (ContentBodyEvaluator is IStringEvaluator rawContentEvaluator)
			{
				eventObject.RawData = await rawContentEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false);
			}

			await executionContext.Send(eventObject, token).ConfigureAwait(false);

			IdLocationEvaluator?.SetValue(new DefaultObject(sendId), executionContext);
		}

		public IContent                           Content          => _send.Content;
		public IValueExpression                   DelayExpression  => _send.DelayExpression;
		public int?                               DelayMs          => _send.DelayMs;
		public string                             Event            => _send.Event;
		public IValueExpression                   EventExpression  => _send.EventExpression;
		public string                             Id               => _send.Id;
		public ILocationExpression                IdLocation       => _send.IdLocation;
		public IReadOnlyList<ILocationExpression> NameList         => _send.NameList;
		public IReadOnlyList<IParam>              Parameters       => _send.Parameters;
		public Uri                                Target           => _send.Target;
		public IValueExpression                   TargetExpression => _send.TargetExpression;
		public Uri                                Type             => _send.Type;
		public IValueExpression                   TypeExpression   => _send.TypeExpression;

		private static Uri ToUri(string uri) => new Uri(uri, UriKind.RelativeOrAbsolute);
	}
}