using System;

namespace TSSArt.StateMachine
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public sealed class SimpleServiceAttribute : Attribute
	{
		public SimpleServiceAttribute(string type)
		{
			if (string.IsNullOrEmpty(type)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(type));

			Type = type;
		}

		public string Type { get; }

		public string Alias { get; set; }
	}
}