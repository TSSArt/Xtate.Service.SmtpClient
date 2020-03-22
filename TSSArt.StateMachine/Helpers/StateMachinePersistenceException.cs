using System;
using System.Runtime.Serialization;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachinePersistenceException : StateMachineException
	{
		public StateMachinePersistenceException()
		{ }

		public StateMachinePersistenceException(string? message) : base(message)
		{ }

		public StateMachinePersistenceException(string? message, Exception? inner) : base(message, inner)
		{ }

		protected StateMachinePersistenceException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }
	}
}