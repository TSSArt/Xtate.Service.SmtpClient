using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public readonly struct LoggerWrapper
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
	}
}