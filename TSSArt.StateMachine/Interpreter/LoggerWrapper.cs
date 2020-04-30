using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal readonly struct LoggerWrapper
	{
		private const string ErrorTypeKey   = "ErrorType";
		private const string ErrorTypeValue = "Platform";
		private const string SessionIdKey   = "SessionId";

		private readonly ILogger _logger;
		private readonly string  _sessionId;

		public LoggerWrapper(ILogger logger, string sessionId)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
		}

		public bool IsPlatformError(Exception exception)
		{
			for (; exception != null; exception = exception.InnerException)
			{
				if (Equals(exception.Data[ErrorTypeKey], ErrorTypeValue) && Equals(exception.Data[SessionIdKey], _sessionId))
				{
					return true;
				}
			}

			return false;
		}

		public async ValueTask Log(string? stateMachineName, string? label, DataModelValue data, CancellationToken token)
		{
			try
			{
				await _logger.LogInfo(_sessionId, stateMachineName, label, data, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MarkAsPlatformError(ex);

				throw;
			}
		}

		public async ValueTask Error(ErrorType errorType, string? stateMachineName, string? sourceEntityId, Exception exception, CancellationToken token)
		{
			try
			{
				await _logger.LogError(errorType, _sessionId, stateMachineName, sourceEntityId, exception, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MarkAsPlatformError(ex);

				throw;
			}
		}

		private void MarkAsPlatformError(Exception exception)
		{
			exception.Data[ErrorTypeKey] = ErrorTypeValue;
			exception.Data[SessionIdKey] = _sessionId;
		}

		public void ProcessingEvent(IEvent evt)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			if (_logger.IsTracingEnabled)
			{
				_logger.TraceProcessingEvent(_sessionId, evt.Type, EventName.ToName(evt.NameParts), evt.SendId, evt.InvokeId,
											 evt.Data, evt.OriginType?.ToString(), evt.Origin?.ToString());
			}
		}

		public void EnteringState(StateEntityNode state)
		{
			if (state == null) throw new ArgumentNullException(nameof(state));

			if (_logger.IsTracingEnabled)
			{
				_logger.TraceEnteringState(_sessionId, state.Id.As<string>());
			}
		}

		public void ExitingState(StateEntityNode state)
		{
			if (state == null) throw new ArgumentNullException(nameof(state));

			if (_logger.IsTracingEnabled)
			{
				_logger.TraceExitingState(_sessionId, state.Id.As<string>());
			}
		}

		public void PerformingTransition(TransitionNode transition)
		{
			if (transition == null) throw new ArgumentNullException(nameof(transition));

			if (_logger.IsTracingEnabled)
			{
				_logger.TracePerformingTransition(_sessionId, transition.Type.ToString(), ToString(transition.EventDescriptors), ToString(transition.Target));
			}
		}

		private static string? ToString(ImmutableArray<IIdentifier> list)
		{
			if (list.IsDefault)
			{
				return null;
			}

			return string.Join(separator: @" ", list.Select(id => id.As<string>()));
		}

		private static string? ToString(ImmutableArray<IEventDescriptor> list)
		{
			if (list.IsDefault)
			{
				return null;
			}

			return string.Join(separator: @" ", list.Select(id => id.As<string>()));
		}
	}
}