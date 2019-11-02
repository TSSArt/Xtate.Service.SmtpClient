using System;
using System.Diagnostics;
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
			_logger = logger;
			_sessionId = sessionId;
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
				if (_logger != null)
				{
					return _logger.Log(stateMachineName, label, data, token);
				}

				FormattableString formattableString = $"Name: [{stateMachineName}]. SessionId: [{_sessionId}]. Label: \"{label}\". Data: {data:JSON}";

				Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());

				return default;
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
				if (_logger != null)
				{
					return _logger.Error(errorType, stateMachineName, sourceEntityId, exception, token);
				}

				FormattableString formattableString = $"Type: [{errorType}]. Name: [{stateMachineName}]. SessionId: [{_sessionId}]. SourceEntityId: [{sourceEntityId}]. Exception: {exception}";

				Trace.TraceError(formattableString.Format, formattableString.GetArguments());

				return default;
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