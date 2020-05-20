using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineResourceNotFoundException : StateMachineException
	{
		public StateMachineResourceNotFoundException() { }

		public StateMachineResourceNotFoundException(string? message) : base(message) { }

		public StateMachineResourceNotFoundException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineResourceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}