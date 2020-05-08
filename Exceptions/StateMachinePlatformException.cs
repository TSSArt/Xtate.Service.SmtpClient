using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachinePlatformException : StateMachineException
	{
		public StateMachinePlatformException() { }

		public StateMachinePlatformException(string message) : base(message) { }

		public StateMachinePlatformException(string message, Exception innerException) : base(message, innerException) { }

		public StateMachinePlatformException(Exception inner, SessionId sessionId) : base(message: null, inner) => SessionId = sessionId;

		protected StateMachinePlatformException(SerializationInfo info, StreamingContext context) : base(info, context) { }

		public SessionId SessionId { get; } = default!;
	}
}