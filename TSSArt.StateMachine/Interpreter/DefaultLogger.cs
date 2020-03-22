using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal sealed class DefaultLogger : ILogger
	{
		public static readonly ILogger Instance = new DefaultLogger();

		private DefaultLogger()
		{ }

		public ValueTask LogInfo(string sessionId, string? stateMachineName, string? label, DataModelValue data, CancellationToken token)
		{
			Trace.TraceInformation(Resources.DefaultLogger_LogInfo, stateMachineName, sessionId, label, data);

			return default;
		}

		public ValueTask LogError(ErrorType errorType, string sessionId, string? stateMachineName, string? sourceEntityId, Exception exception, CancellationToken token)
		{
			Trace.TraceError(Resources.DefaultLogger_LogError, errorType, stateMachineName, sessionId, sourceEntityId, exception);

			return default;
		}

#if TRACE
		public bool IsTracingEnabled => true;
#else
		public bool IsTracingEnabled => false;
#endif

		public void TraceEnteringState(string sessionId, string stateId)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TraceEnteringState, stateId, sessionId);
		}

		public void TraceExitingState(string sessionId, string stateId)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TraceExitingState, stateId, sessionId);
		}

		public void TracePerformingTransition(string sessionId, string type, string? evt, string? target)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TracePerformingTransition, type, target, evt, sessionId);
		}

		public void TraceProcessingEvent(string sessionId, EventType eventType, string name, string? sendId, string? invokeId, DataModelValue data, string? originType, string? origin)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TraceProcessingEvent, eventType, name, sendId, invokeId, data, originType, origin, sessionId);
		}
	}
}