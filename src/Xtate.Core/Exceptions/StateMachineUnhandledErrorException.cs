using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineUnhandledErrorException : StateMachineException
	{
		public StateMachineUnhandledErrorException() { }

		public StateMachineUnhandledErrorException(string? message) : base(message) { }

		public StateMachineUnhandledErrorException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineUnhandledErrorException(SerializationInfo info, StreamingContext context) : base(info, context) { }

		public StateMachineUnhandledErrorException(string message, Exception? innerException, UnhandledErrorBehaviour unhandledErrorBehaviour) : base(message, innerException) =>
				UnhandledErrorBehaviour = unhandledErrorBehaviour;

		public UnhandledErrorBehaviour UnhandledErrorBehaviour { get; }
	}
}