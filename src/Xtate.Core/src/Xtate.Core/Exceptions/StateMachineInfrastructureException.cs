using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineInfrastructureException : StateMachineException
	{
		public StateMachineInfrastructureException() { }

		public StateMachineInfrastructureException(string? message) : base(message) { }

		public StateMachineInfrastructureException(string? message, Exception? inner) : base(message, inner) { }

		protected StateMachineInfrastructureException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}