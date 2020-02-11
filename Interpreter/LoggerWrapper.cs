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

		public ValueTask Log(string stateMachineName, string label, DataModelValue data, CancellationToken token)
		{
			try
			{
				return _logger.Log(_sessionId, stateMachineName, label, data, token);
			}
			catch (Exception ex)
			{
				MarkAsPlatformError(ex);

				throw;
			}
		}

		public ValueTask Error(ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token)
		{
			try
			{
				return _logger.Error(errorType, _sessionId, stateMachineName, sourceEntityId, exception, token);
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

		public void ProcessingEvent(IEvent @event)
		{
			if (_logger.IsTracingEnabled)
			{
				if (@event == null) throw new ArgumentNullException(nameof(@event));

				_logger.TraceProcessingEvent(_sessionId, @event.Type, EventName.ToName(@event.NameParts), @event.SendId, @event.InvokeId,
											 @event.Data, @event.OriginType?.ToString(), @event.Origin?.ToString());
			}
		}

		public void EnteringState(StateEntityNode state)
		{
			if (_logger.IsTracingEnabled)
			{
				if (state == null) throw new ArgumentNullException(nameof(state));

				_logger.TraceEnteringState(_sessionId, state.Id.Base<IIdentifier>().ToString());
			}
		}

		public void ExitingState(StateEntityNode state)
		{
			if (_logger.IsTracingEnabled)
			{
				if (state == null) throw new ArgumentNullException(nameof(state));

				_logger.TraceExitingState(_sessionId, state.Id.Base<IIdentifier>().ToString());
			}
		}

		public void PerformingTransition(TransitionNode transition)
		{
			if (_logger.IsTracingEnabled)
			{
				if (transition == null) throw new ArgumentNullException(nameof(transition));

				_logger.TracePerformingTransition(_sessionId, transition.Type.ToString(), ToString(transition.Event), ToString(transition.Target));
			}
		}

		private static string ToString(ImmutableArray<IIdentifier> list)
		{
			if (list == null)
			{
				return null;
			}

			return string.Join(separator: " ", list.Select(id => id.Base<IIdentifier>().ToString()));
		}

		private static string ToString(ImmutableArray<IEventDescriptor> list)
		{
			if (list == null)
			{
				return null;
			}

			return string.Join(separator: " ", list.Select(id => id.Base<IEventDescriptor>().ToString()));
		}
	}
}