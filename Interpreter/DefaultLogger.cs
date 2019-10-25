using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultLogger : ILogger
	{
		public static readonly ILogger Instance = new DefaultLogger();

		public Task Error(ErrorType errorType, string sessionId, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token)
		{
			FormattableString formattableString = $"Type: [{errorType}]. Name: [{stateMachineName}]. SessionId: [{sessionId}]. SourceEntityId: [{sourceEntityId}]. Exception: {exception}";

			Trace.TraceError(formattableString.Format, formattableString.GetArguments());

			return Task.CompletedTask;
		}

		public Task Log(string sessionId, string stateMachineName, string label, object data, CancellationToken token)
		{
			FormattableString formattableString;

			if (string.IsNullOrEmpty(label))
			{
				formattableString = $"Name: [{stateMachineName}]. SessionId: [{sessionId}]. Message: \"{data}\"";
			}
			else if (data == null)
			{
				formattableString = $"Name: [{stateMachineName}]. SessionId: [{sessionId}]. Message: \"{label}\"";
			}
			else
			{
				formattableString = $"Name: [{stateMachineName}]. SessionId: [{sessionId}]. Label: \"{label}\". Data: {{{data}}}";
			}

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());

			return Task.CompletedTask;
		}
	}
}