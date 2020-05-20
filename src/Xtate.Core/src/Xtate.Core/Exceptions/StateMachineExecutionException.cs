using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineExecutionException : StateMachineException
	{
		public StateMachineExecutionException() { }

		public StateMachineExecutionException(string? message) : base(message) { }

		public StateMachineExecutionException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineExecutionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}