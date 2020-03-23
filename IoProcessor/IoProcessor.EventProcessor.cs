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

	#region Interface IEventConsumer

		ValueTask IEventConsumer.Dispatch(string sessionId, IEvent evt, CancellationToken token)
		{
			GetCurrentContext().ValidateSessionId(sessionId, out var controller);

			return controller.Send(evt, token);
		}

	#endregion

	#region Interface IEventProcessor

		Uri IEventProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		ValueTask IEventProcessor.Dispatch(string sessionId, IOutgoingEvent evt, CancellationToken token)
		{
			if (evt.Target == null)
			{
				throw new StateMachineProcessorException(Resources.Exception_Event_Target_did_not_specified);
			}

			var service = GetCurrentContext().GetService(sessionId, evt.Target);

			var serviceEvent = new EventObject(EventType.External, evt, GetTarget(sessionId), EventProcessorId);

			return service.Send(serviceEvent, token);
		}

		Uri IEventProcessor.Id => EventProcessorId;

		Uri IEventProcessor.AliasId => EventProcessorAliasId;

	#endregion

		private static Uri GetTarget(string sessionId) => new Uri(BaseUri, sessionId);
	}
}