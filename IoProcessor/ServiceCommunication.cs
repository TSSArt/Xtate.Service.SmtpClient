using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class ServiceCommunication : IServiceCommunication
	{
		private readonly StateMachineController _creator;
		private readonly string                 _invokeId;
		private readonly string                 _invokeUniqueId;
		private readonly Uri                    _originType;
		private          Uri                    _origin;

		public ServiceCommunication(StateMachineController creator, Uri originType, string invokeId, string invokeUniqueId)
		{
			_creator = creator;
			_originType = originType;
			_invokeId = invokeId;
			_invokeUniqueId = invokeUniqueId;
		}

		public ValueTask SendToCreator(IOutgoingEvent @event, CancellationToken token)
		{
			if (@event.Type != null || @event.SendId != null || @event.DelayMs != 0)
			{
				throw new InvalidOperationException("Type, SendId, DelayMs can't be specified for this event");
			}

			if (@event.Target != Event.ParentTarget && @event.Target != null)
			{
				throw new InvalidOperationException("Target should be equal to '_parent' or null");
			}

			_origin ??= new Uri("#_" + _invokeId);

			var eventObject = new EventObject(EventType.External, @event, _origin, _originType, _invokeId, _invokeUniqueId);

			return _creator.Send(eventObject, token);
		}
	}
}