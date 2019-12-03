using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ILogger
	{
		ValueTask Log(string sessionId, string stateMachineName, string label, DataModelValue data, CancellationToken token);
		ValueTask Error(ErrorType errorType, string sessionId, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token);

		bool IsTracingEnabled { get; }
		void TraceProcessingEvent(string sessionId, EventType eventType, string name, string sendId, string invokeId, DataModelValue data, string originType, string origin);
		void TraceEnteringState(string sessionId, string stateId);
		void TraceExitingState(string sessionId, string stateId);
		void TracePerformingTransition(string sessionId, string type, string @event, string target);
	}
}