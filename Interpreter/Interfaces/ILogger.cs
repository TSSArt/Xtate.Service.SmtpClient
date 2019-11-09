using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ILogger
	{
		ValueTask Log(string stateMachineName, string label, DataModelValue data, CancellationToken token);
		ValueTask Error(ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token);
	}
}