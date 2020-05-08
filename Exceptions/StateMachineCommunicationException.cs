using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineCommunicationException : StateMachineException
	{
		public StateMachineCommunicationException() { }

		public StateMachineCommunicationException(string message) : base(message) { }

		public StateMachineCommunicationException(string message, Exception innerException) : base(message, innerException) { }

		public StateMachineCommunicationException(Exception inner, SessionId sessionId, SendId? sendId = default) : base(message: null, inner)
		{
			SessionId = sessionId;
			SendId = sendId;
		}

		protected StateMachineCommunicationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

		public SessionId SessionId { get; } = default!;

		public SendId? SendId { get; }
	}
}