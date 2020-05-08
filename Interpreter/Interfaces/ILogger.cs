using System;
using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface ILogger
	{
		bool      IsTracingEnabled { get; }
		ValueTask LogInfo(SessionId sessionId, string? stateMachineName, string? label, DataModelValue data, CancellationToken token);
		ValueTask LogError(ErrorType errorType, SessionId sessionId, string? stateMachineName, string? sourceEntityId, Exception exception, CancellationToken token);
		void      TraceProcessingEvent(SessionId sessionId, EventType eventType, string name, SendId? sendId, InvokeId? invokeId, DataModelValue data, string? originType, string? origin);
		void      TraceEnteringState(SessionId sessionId, IIdentifier stateId);
		void      TraceExitingState(SessionId sessionId, IIdentifier stateId);
		void      TracePerformingTransition(SessionId sessionId, string type, string? evt, string? target);
	}
}