using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class IoProcessor : IEventProcessor, IEventConsumer
	{
		private static readonly Uri BaseUri               = new Uri("ioprocessor://./");
		private static readonly Uri EventProcessorId      = new Uri("http://www.w3.org/TR/scxml/#SCXMLEventProcessor");
		private static readonly Uri EventProcessorAliasId = new Uri(uriString: "scxml", UriKind.Relative);

		ValueTask IEventConsumer.Dispatch(string sessionId, IEvent @event, CancellationToken token)
		{
			_context.ValidateSessionId(sessionId, out var controller);

			return controller.Send(@event, token);
		}

		Uri IEventProcessor.Id => EventProcessorId;

		Uri IEventProcessor.AliasId => EventProcessorAliasId;

		Uri IEventProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		ValueTask IEventProcessor.Dispatch(string sessionId, IOutgoingEvent @event, CancellationToken token)
		{
			var service = _context.GetService(sessionId, @event.Target);

			var serviceEvent = new EventObject(EventType.External, @event, GetTarget(sessionId), EventProcessorId);

			return service.Send(serviceEvent, token);
		}

		private static Uri GetTarget(string sessionId) => new Uri(BaseUri, sessionId);
	}
}