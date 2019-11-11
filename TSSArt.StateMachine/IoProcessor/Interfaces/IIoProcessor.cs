using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal interface IIoProcessor
	{
		Uri                            GetTarget(string sessionId);
		IReadOnlyList<IEventProcessor> GetIoProcessors();
		ValueTask<SendStatus>          DispatchEvent(string sessionId, IOutgoingEvent @event, CancellationToken token);
		ValueTask                      StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue content, DataModelValue parameters, CancellationToken token);
		ValueTask                      CancelInvoke(string sessionId, string invokeId, CancellationToken token);
		bool                           IsInvokeActive(string sessionId, string invokeId);
		ValueTask                      ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token);
		ValueTask                      Log(string sessionId, string stateMachineName, string label, DataModelValue data, CancellationToken token);
		ValueTask                      Error(string sessionId, ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token);
	}
}