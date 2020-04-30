using System;
using System.Runtime.Serialization;
using System.Threading;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineDestroyedException : OperationCanceledException
	{
		public StateMachineDestroyedException() { }

		public StateMachineDestroyedException(string? message) : base(message) { }

		public StateMachineDestroyedException(string? message, Exception? inner) : base(message, inner) { }

		public StateMachineDestroyedException(string? message, Exception? inner, CancellationToken token) : base(message, inner, token) { }

		protected StateMachineDestroyedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}