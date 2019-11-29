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
	}
}