using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ILogger
	{
		ValueTask Log(string sessionId, string stateMachineName, string label, object data, CancellationToken token);
		ValueTask Error(ErrorType errorType, string sessionId, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token);
	}
}