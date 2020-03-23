using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface ILogger
	{
		bool      IsTracingEnabled { get; }
		ValueTask LogInfo(string sessionId, string? stateMachineName, string? label, DataModelValue data, CancellationToken token);
		ValueTask LogError(ErrorType errorType, string sessionId, string? stateMachineName, string? sourceEntityId, Exception exception, CancellationToken token);
		void      TraceProcessingEvent(string sessionId, EventType eventType, string name, string? sendId, string? invokeId, DataModelValue data, string? originType, string? origin);
		void      TraceEnteringState(string sessionId, string stateId);
		void      TraceExitingState(string sessionId, string stateId);
		void      TracePerformingTransition(string sessionId, string type, string? evt, string? target);
	}
}