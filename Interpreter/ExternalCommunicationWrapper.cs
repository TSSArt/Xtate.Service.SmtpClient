using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal readonly struct ExternalCommunicationWrapper
	{
		private readonly IExternalCommunication? _externalCommunication;
		private readonly SessionId               _sessionId;

		public ExternalCommunicationWrapper(IExternalCommunication? externalCommunication, SessionId sessionId)
		{
			_externalCommunication = externalCommunication;
			_sessionId = sessionId;
		}

		public ImmutableArray<IIoProcessor> GetIoProcessors() => _externalCommunication?.GetIoProcessors() ?? default;

		public bool IsCommunicationError(Exception exception, out SendId? sendId)
		{
			for (; exception != null; exception = exception.InnerException)
			{
				if (exception is StateMachineCommunicationException ex && ex.SessionId == _sessionId)
				{
					sendId = ex.SendId;

					return true;
				}
			}

			sendId = null;

			return false;
		}

		public async ValueTask StartInvoke(InvokeData invokeData, CancellationToken token)
		{
			try
			{
				if (_externalCommunication == null)
				{
					throw NoExternalCommunication();
				}

				await _externalCommunication.StartInvoke(invokeData, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new StateMachineCommunicationException(ex, _sessionId);
			}
		}

		public async ValueTask CancelInvoke(InvokeId invokeId, CancellationToken token)
		{
			try
			{
				if (_externalCommunication == null)
				{
					throw NoExternalCommunication();
				}

				await _externalCommunication.CancelInvoke(invokeId, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new StateMachineCommunicationException(ex, _sessionId);
			}
		}

		public bool IsInvokeActive(InvokeId invokeId) => _externalCommunication?.IsInvokeActive(invokeId) == true;

		public async ValueTask<SendStatus> TrySendEvent(IOutgoingEvent evt, CancellationToken token)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			try
			{
				if (_externalCommunication == null)
				{
					throw NoExternalCommunication();
				}

				return await _externalCommunication.TrySendEvent(evt, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new StateMachineCommunicationException(ex, _sessionId, evt.SendId);
			}
		}

		public async ValueTask ForwardEvent(IEvent evt, InvokeId invokeId, CancellationToken token)
		{
			try
			{
				if (_externalCommunication == null)
				{
					throw NoExternalCommunication();
				}

				await _externalCommunication.ForwardEvent(evt, invokeId, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new StateMachineCommunicationException(ex, _sessionId);
			}
		}

		public async ValueTask CancelEvent(SendId sendId, CancellationToken token)
		{
			try
			{
				if (_externalCommunication == null)
				{
					throw NoExternalCommunication();
				}

				await _externalCommunication.CancelEvent(sendId, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new StateMachineCommunicationException(ex, _sessionId, sendId);
			}
		}

		private static NotSupportedException NoExternalCommunication() => new NotSupportedException(Resources.Exception_External_communication_does_not_configured_for_state_machine_interpreter);
	}
}