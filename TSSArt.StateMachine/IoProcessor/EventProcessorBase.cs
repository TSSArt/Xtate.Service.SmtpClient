using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public abstract class EventProcessorBase : IEventProcessor
	{
		private readonly IEventConsumer _eventConsumer;
		private readonly Uri?           _eventProcessorAliasId;

		protected EventProcessorBase(IEventConsumer eventConsumer)
		{
			_eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));

			var eventProcessorAttribute = GetType().GetCustomAttribute<EventProcessorAttribute>(false);

			if (eventProcessorAttribute == null)
			{
				throw new StateMachineInfrastructureException(Res.Format(Resources.Exception_EventProcessorAttributeWasNotProvided, GetType()));
			}

			EventProcessorId = new Uri(eventProcessorAttribute.Type, UriKind.RelativeOrAbsolute);
			_eventProcessorAliasId = eventProcessorAttribute.Alias != null ? new Uri(eventProcessorAttribute.Alias, UriKind.RelativeOrAbsolute) : null;
		}

		protected Uri EventProcessorId { get; }

	#region Interface IEventProcessor

		Uri IEventProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		ValueTask IEventProcessor.Dispatch(string sessionId, IOutgoingEvent evt, CancellationToken token) => OutgoingEvent(sessionId, evt, token);

		bool IEventProcessor.CanHandle(Uri? type, Uri? target) => FullUriComparer.Instance.Equals(type, EventProcessorId) || FullUriComparer.Instance.Equals(type, _eventProcessorAliasId);

		Uri IEventProcessor.Id => EventProcessorId;

	#endregion

		protected abstract Uri GetTarget(string sessionId);

		protected abstract ValueTask OutgoingEvent(string sessionId, IOutgoingEvent evt, CancellationToken token);

		protected ValueTask IncomingEvent(string sessionId, IEvent evt, CancellationToken token) => _eventConsumer.Dispatch(sessionId, evt, token);
	}
}