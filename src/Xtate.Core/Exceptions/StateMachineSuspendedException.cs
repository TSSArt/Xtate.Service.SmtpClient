using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineSuspendedException : StateMachineException
	{
		public StateMachineSuspendedException() { }

		public StateMachineSuspendedException(string? message) : base(message) { }

		public StateMachineSuspendedException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineSuspendedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}