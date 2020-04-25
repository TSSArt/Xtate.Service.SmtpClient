using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Text;

namespace TSSArt.StateMachine
{
	[Serializable]
	public class StateMachineValidationException : StateMachineException
	{
		protected StateMachineValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

		public StateMachineValidationException(ImmutableArray<ErrorItem> validationMessages, string? sessionId = null, IStateMachine? stateMachine = null, Uri? source = null, string? scxml = null)
				: base(GetMessage(validationMessages))
		{
			SessionId = sessionId;
			StateMachine = stateMachine;
			ScxmlSource = source;
			Scxml = scxml;
			ValidationMessages = validationMessages;
		}

		public Uri?                      ScxmlSource        { get; }
		public string?                   SessionId          { get; }
		public IStateMachine?            StateMachine       { get; }
		public string?                   Scxml              { get; }
		public ImmutableArray<ErrorItem> ValidationMessages { get; }

		private static string? GetMessage(ImmutableArray<ErrorItem> validationMessages)
		{
			if (validationMessages.IsDefaultOrEmpty)
			{
				return null;
			}

			if (validationMessages.Length == 1)
			{
				return validationMessages[0].ToString();
			}

			var sb = new StringBuilder();
			var index = 1;
			foreach (var error in validationMessages)
			{
				if (index > 1)
				{
					sb.AppendLine();
				}

				sb.Append(Res.Format(Resources.Exception_StateMachineValidationException_Message, index ++, validationMessages.Length, error));
			}

			return sb.ToString();
		}
	}
}