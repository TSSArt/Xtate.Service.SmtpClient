using System;

namespace TSSArt.StateMachine
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public sealed class CustomActionProviderAttribute : Attribute
	{
		public CustomActionProviderAttribute(string @namespace)
		{
			if (string.IsNullOrEmpty(@namespace)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(@namespace));
			
			Namespace = @namespace;
		}

		public string Namespace { get; }
	}
}