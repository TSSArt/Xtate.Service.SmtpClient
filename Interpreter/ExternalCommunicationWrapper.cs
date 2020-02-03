﻿using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public readonly struct ExternalCommunicationWrapper
	{
		private const string ErrorTypeKey   = "ErrorType";
		private const string ErrorTypeValue = "Communication";
		private const string SessionIdKey   = "SessionId";
		private const string SendIdKey      = "SendId";

		private readonly IExternalCommunication _externalCommunication;
		private readonly string                 _sessionId;

		public ExternalCommunicationWrapper(IExternalCommunication externalCommunication, string sessionId)
		{
			_externalCommunication = externalCommunication;
			_sessionId = sessionId;
		}

		public ImmutableArray<IEventProcessor> GetIoProcessors() => _externalCommunication?.GetIoProcessors() ?? Array.Empty<IEventProcessor>();

		public bool IsCommunicationError(Exception exception, out string sendId)
		{
			for (; exception != null; exception = exception.InnerException)
			{
				if (Equals(exception.Data[ErrorTypeKey], ErrorTypeValue) && Equals(exception.Data[SessionIdKey], _sessionId))
				{
					sendId = (string) exception.Data[SendIdKey];

					return true;
				}
			}

			sendId = null;

			return false;
		}

		private void MarkAsCommunicationError(Exception exception, string sendId = null)
		{
			exception.Data[ErrorTypeKey] = ErrorTypeValue;
			exception.Data[SessionIdKey] = _sessionId;

			if (sendId != null)
			{
				exception.Data[SendIdKey] = sendId;
			}
		}

		public async ValueTask StartInvoke(InvokeData invokeData, CancellationToken token)
		{
			try
			{
				ThrowIfNoExternalCommunication();

				await _externalCommunication.StartInvoke(invokeData, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MarkAsCommunicationError(ex);
				throw;
			}
		}

		public async ValueTask CancelInvoke(string invokeId, CancellationToken token)
		{
			try
			{
				ThrowIfNoExternalCommunication();

				await _externalCommunication.CancelInvoke(invokeId, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MarkAsCommunicationError(ex);
				throw;
			}
		}

		public bool IsInvokeActive(string invokeId, string invokeUniqueId) => _externalCommunication?.IsInvokeActive(invokeId, invokeUniqueId) == true;

		public async ValueTask<SendStatus> TrySendEvent(IOutgoingEvent @event, CancellationToken token)
		{
			if (@event == null) throw new ArgumentNullException(nameof(@event));

			try
			{
				ThrowIfNoExternalCommunication();

				return await _externalCommunication.TrySendEvent(@event, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MarkAsCommunicationError(ex, @event.SendId);
				throw;
			}
		}

		public async ValueTask ForwardEvent(IEvent @event, string invokeId, CancellationToken token)
		{
			try
			{
				ThrowIfNoExternalCommunication();

				await _externalCommunication.ForwardEvent(@event, invokeId, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MarkAsCommunicationError(ex);
				throw;
			}
		}

		public async ValueTask CancelEvent(string sendId, CancellationToken token)
		{
			try
			{
				ThrowIfNoExternalCommunication();

				await _externalCommunication.CancelEvent(sendId, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MarkAsCommunicationError(ex, sendId);
				throw;
			}
		}

		private void ThrowIfNoExternalCommunication()
		{
			if (_externalCommunication == null)
			{
				throw new NotSupportedException("External communication does not configured for state machine interpreter");
			}
		}
	}
}