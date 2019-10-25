using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class ExternalCommunicationWrapper
	{
		private const string SendIdKey = "SendId";
		private readonly IExternalCommunication _inner;

		public ExternalCommunicationWrapper(IExternalCommunication inner) => _inner = inner;

		public IReadOnlyList<IEventProcessor> GetIoProcessors(string sessionId) => _inner.GetIoProcessors(sessionId);

		public bool IsCommunicationError(Exception exception, out string sendId)
		{
			for (; exception != null; exception = exception.InnerException)
			{
				if (!exception.Data.Contains(typeof(ExternalCommunicationWrapper)))
				{
					continue;
				}

				if (exception.Data[typeof(ExternalCommunicationWrapper)] is ExternalCommunicationWrapper wrapper)
				{
					sendId = exception.Data[SendIdKey] as string;
					return ReferenceEquals(wrapper, this);
				}
			}

			sendId = null;

			return false;
		}

		public async Task StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token)
		{
			try
			{
				await _inner.StartInvoke(sessionId, invokeId, type, source, data, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(typeof(ExternalCommunicationWrapper), this);
				throw;
			}
		}

		public async Task CancelInvoke(string sessionId, string invokeId, CancellationToken token)
		{
			try
			{
				await _inner.CancelInvoke(sessionId, invokeId, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(typeof(ExternalCommunicationWrapper), this);
				throw;
			}
		}

		public async Task SendEvent(string sessionId, IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token)
		{
			try
			{
				await _inner.SendEvent(sessionId, @event, type, target, delayMs, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(typeof(ExternalCommunicationWrapper), this);
				ex.Data.Add(SendIdKey, @event.SendId);
				throw;
			}
		}

		public async Task ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token)
		{
			try
			{
				await _inner.ForwardEvent(sessionId, @event, invokeId, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(typeof(ExternalCommunicationWrapper), this);
				throw;
			}
		}

		public async Task CancelEvent(string sessionId, string sendId, CancellationToken token)
		{
			try
			{
				await _inner.CancelEvent(sessionId, sendId, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(typeof(ExternalCommunicationWrapper), this);
				ex.Data.Add(SendIdKey, sendId);
				throw;
			}
		}

		public async Task ReturnDoneEvent(string sessionId, DataModelValue doneData, CancellationToken token)
		{
			try
			{
				await _inner.ReturnDoneEvent(sessionId, doneData, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(typeof(ExternalCommunicationWrapper), this);
				throw;
			}
		}
	}
}