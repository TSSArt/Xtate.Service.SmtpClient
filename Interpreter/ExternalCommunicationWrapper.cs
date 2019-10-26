using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public readonly struct ExternalCommunicationWrapper
	{
		private const string ExternalCommunicationKey = "ExternalCommunication";
		private const string SendIdKey                = "SendId";

		private readonly IExternalCommunication _externalCommunication;

		public ExternalCommunicationWrapper(IExternalCommunication externalCommunication) => _externalCommunication = externalCommunication;

		public IReadOnlyList<IEventProcessor> GetIoProcessors(string sessionId) => _externalCommunication.GetIoProcessors(sessionId);

		public readonly bool IsCommunicationError(Exception exception, out string sendId)
		{
			for (; exception != null; exception = exception.InnerException)
			{
				if (!exception.Data.Contains(ExternalCommunicationKey))
				{
					continue;
				}

				if (exception.Data[ExternalCommunicationKey] is IExternalCommunication inst)
				{
					sendId = exception.Data[SendIdKey] as string;
					return ReferenceEquals(inst, _externalCommunication);
				}
			}

			sendId = null;

			return false;
		}

		public async ValueTask StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token)
		{
			try
			{
				await _externalCommunication.StartInvoke(sessionId, invokeId, type, source, data, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(ExternalCommunicationKey, _externalCommunication);
				throw;
			}
		}

		public async ValueTask CancelInvoke(string sessionId, string invokeId, CancellationToken token)
		{
			try
			{
				await _externalCommunication.CancelInvoke(sessionId, invokeId, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(ExternalCommunicationKey, _externalCommunication);
				throw;
			}
		}

		public async ValueTask SendEvent(string sessionId, IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token)
		{
			try
			{
				await _externalCommunication.SendEvent(sessionId, @event, type, target, delayMs, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(ExternalCommunicationKey, _externalCommunication);
				ex.Data.Add(SendIdKey, @event.SendId);
				throw;
			}
		}

		public async ValueTask ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token)
		{
			try
			{
				await _externalCommunication.ForwardEvent(sessionId, @event, invokeId, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(ExternalCommunicationKey, _externalCommunication);
				throw;
			}
		}

		public async ValueTask CancelEvent(string sessionId, string sendId, CancellationToken token)
		{
			try
			{
				await _externalCommunication.CancelEvent(sessionId, sendId, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(ExternalCommunicationKey, _externalCommunication);
				ex.Data.Add(SendIdKey, sendId);
				throw;
			}
		}

		public async ValueTask ReturnDoneEvent(string sessionId, DataModelValue doneData, CancellationToken token)
		{
			try
			{
				await _externalCommunication.ReturnDoneEvent(sessionId, doneData, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(ExternalCommunicationKey, _externalCommunication);
				throw;
			}
		}
	}
}