using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineQueueClosedException : StateMachineException
	{
		public StateMachineQueueClosedException() { }

		public StateMachineQueueClosedException(string? message) : base(message) { }

		public StateMachineQueueClosedException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineQueueClosedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}