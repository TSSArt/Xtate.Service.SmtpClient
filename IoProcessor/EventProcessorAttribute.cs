using System;

namespace TSSArt.StateMachine
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class EventProcessorAttribute : Attribute
	{
		public EventProcessorAttribute(string type)
		{
			if (string.IsNullOrEmpty(type)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(type));

			Type = type;
		}

		public string Type { get; }

		public string? Alias { get; set; }
	}
}