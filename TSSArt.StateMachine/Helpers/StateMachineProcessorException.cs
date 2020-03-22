using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineProcessorException : StateMachineException
	{
		public StateMachineProcessorException()
		{ }

		public StateMachineProcessorException(string? message) : base(message)
		{ }

		public StateMachineProcessorException(string? message, Exception? inner) : base(message, inner)
		{ }

		protected StateMachineProcessorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }
	}
}