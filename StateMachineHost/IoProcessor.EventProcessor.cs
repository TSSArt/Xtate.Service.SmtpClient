using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class IoProcessor : IEventProcessor, IEventConsumer
	{
		private static readonly Uri BaseUri               = new Uri("ioprocessor:///");
		private static readonly Uri EventProcessorId      = new Uri("http://www.w3.org/TR/scxml/#SCXMLEventProcessor");
		private static readonly Uri EventProcessorAliasId = new Uri(uriString: "scxml", UriKind.Relative);

	#region Interface IEventConsumer

		public ValueTask Dispatch(string sessionId, IEvent evt, CancellationToken token = default)
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

			var service = GetCurrentContext().GetService(sessionId, new Uri(evt.Target.Fragment));

			var serviceEvent = new EventObject(EventType.External, evt, GetTarget(sessionId), EventProcessorId);

			return service.Send(serviceEvent, token);
		}

		bool IEventProcessor.CanHandle(Uri? type, Uri? target) => CanHandleType(type) && CanHandleTarget(target);

		Uri IEventProcessor.Id => EventProcessorId;

	#endregion

		private bool CanHandleType(Uri? type) => type == null || FullUriComparer.Instance.Equals(type, EventProcessorId) || FullUriComparer.Instance.Equals(type, EventProcessorAliasId);

		private bool CanHandleTarget(Uri? target)
		{
			if (target == null)
			{
				return true;
			}

			if (target.IsAbsoluteUri && target.IsLoopback && target.GetComponents(UriComponents.Path, UriFormat.Unescaped).Length == 0)
			{
				return true;
			}

			return !target.IsAbsoluteUri;
		}

		private static Uri GetTarget(string sessionId) => new Uri(BaseUri, "#_scxml_" + sessionId);
	}
}