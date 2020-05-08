using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal sealed class DefaultLogger : ILogger
	{
		public static readonly ILogger Instance = new DefaultLogger();

		private DefaultLogger() { }

		public ValueTask LogInfo(SessionId sessionId, string? stateMachineName, string? label, DataModelValue data, CancellationToken token)
		{
			Trace.TraceInformation(Resources.DefaultLogger_LogInfo, stateMachineName, sessionId.Value, label, data);

			return default;
		}

		public ValueTask LogError(ErrorType errorType, SessionId sessionId, string? stateMachineName, string? sourceEntityId, Exception exception, CancellationToken token)
		{
			Trace.TraceError(Resources.DefaultLogger_LogError, errorType, stateMachineName, sessionId.Value, sourceEntityId, exception);

			return default;
		}

#if DEBUG
		public bool IsTracingEnabled => true;
#else
		public bool IsTracingEnabled => false;
#endif

		public void TraceEnteringState(SessionId sessionId, IIdentifier stateId)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TraceEnteringState, stateId.Value, sessionId.Value);
		}

		public void TraceExitingState(SessionId sessionId, IIdentifier stateId)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TraceExitingState, stateId.Value, sessionId.Value);
		}

		public void TracePerformingTransition(SessionId sessionId, string type, string? evt, string? target)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TracePerformingTransition, type, target, evt, sessionId.Value);
		}

		public void TraceProcessingEvent(SessionId sessionId, EventType eventType, string name, SendId? sendId, InvokeId? invokeId, DataModelValue data, string? originType, string? origin)
		{
			Trace.TraceInformation(Resources.DefaultLogger_TraceProcessingEvent, eventType, name, sendId, invokeId, data, originType, origin, sessionId.Value);
		}
	}
}