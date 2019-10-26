using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultExternalCommunication : IExternalCommunication
	{
		public static readonly IExternalCommunication Instance = new DefaultExternalCommunication();

		public IReadOnlyList<IEventProcessor> GetIoProcessors(string sessionId) => Array.Empty<IEventProcessor>();

		public ValueTask StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token) => throw GetNotSupportedException();

		public ValueTask CancelInvoke(string sessionId, string invokeId, CancellationToken token) => throw GetNotSupportedException();

		public ValueTask ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token) => throw GetNotSupportedException();

		public ValueTask SendEvent(string sessionId, IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token) => throw GetNotSupportedException();

		public ValueTask CancelEvent(string sessionId, string sendId, CancellationToken token) => throw GetNotSupportedException();

		public ValueTask ReturnDoneEvent(string sessionId, DataModelValue evaluateDoneData, CancellationToken token) => default;

		private static Exception GetNotSupportedException() => new NotSupportedException("External communication does not configured for state machine interpreter");
	}
}