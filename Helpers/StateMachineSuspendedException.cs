using System;
using System.Runtime.Serialization;
using System.Threading;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineSuspendedException : OperationCanceledException
	{
		public StateMachineSuspendedException() { }

		public StateMachineSuspendedException(string? message) : base(message) { }

		public StateMachineSuspendedException(string? message, Exception? inner) : base(message, inner) { }

		public StateMachineSuspendedException(string? message, Exception? inner, CancellationToken token) : base(message, inner, token) { }

		protected StateMachineSuspendedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}