using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineException : Exception
	{
		public StateMachineException() { }

		public StateMachineException(string? message) : base(message) { }

		public StateMachineException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}