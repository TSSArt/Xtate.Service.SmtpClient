using System;
using System.Runtime.Serialization;
using System.Threading;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineUnhandledErrorException : OperationCanceledException
	{
		public StateMachineUnhandledErrorException() { }

		public StateMachineUnhandledErrorException(string? message) : base(message) { }

		public StateMachineUnhandledErrorException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineUnhandledErrorException(SerializationInfo info, StreamingContext context) : base(info, context) { }

		public StateMachineUnhandledErrorException(string message, Exception? innerException, CancellationToken token) : base(message, innerException, token) { }
	}
}