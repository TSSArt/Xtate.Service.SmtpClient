using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class DefaultLogger : ILogger
	{
		public static readonly ILogger Instance = new DefaultLogger();

		private DefaultLogger() { }

		public ValueTask Log(string sessionId, string stateMachineName, string label, DataModelValue data, CancellationToken token)
		{
			FormattableString formattableString = $"Name: [{stateMachineName}]. SessionId: [{sessionId}]. Label: \"{label}\". Data: {data:JSON}";

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());

			return default;
		}

		public ValueTask Error(ErrorType errorType, string sessionId, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token)
		{
			FormattableString formattableString = $"Type: [{errorType}]. Name: [{stateMachineName}]. SessionId: [{sessionId}]. SourceEntityId: [{sourceEntityId}]. Exception: {exception}";

			Trace.TraceError(formattableString.Format, formattableString.GetArguments());

			return default;
		}

#if TRACE
		public bool IsTracingEnabled => true;
#else
		public bool IsTracingEnabled => false;
#endif

		public void TraceEnteringState(string sessionId, string stateId)
		{
			FormattableString formattableString = $"Entering to state: [{stateId}]. SessionId: [{sessionId}].";

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());
		}

		public void TraceExitingState(string sessionId, string stateId)
		{
			FormattableString formattableString = $"Exiting from state: [{stateId}]. SessionId: [{sessionId}].";

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());
		}

		public void TracePerformingTransition(string sessionId, string type, string @event, string target)
		{
			FormattableString formattableString = $"Performing {type} transition to: [{target}]. Event: [{@event}]. SessionId: [{sessionId}].";

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());
		}

		public void TraceProcessingEvent(string sessionId, EventType eventType, string name, string sendId, string invokeId, DataModelValue data, string originType, string origin)
		{
			FormattableString formattableString = $"Processing {eventType} event [{name}]. SendId: [{sendId}]. InvokeId: [{invokeId}]. Data: [{data:JSON}]. OriginType: [{originType}]. Origin: [{origin}]. SessionId: [{sessionId}].";

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());
		}
	}
}