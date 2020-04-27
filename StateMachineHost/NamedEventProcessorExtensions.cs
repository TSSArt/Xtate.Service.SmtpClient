using System;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public static class NamedEventProcessorExtensions
	{
		public static StateMachineHostBuilder AddNamedEventProcessor(this StateMachineHostBuilder builder, string name)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new NamedEventProcessorFactory(name));

			return builder;
		}

		public static StateMachineHostBuilder AddNamedEventProcessor(this StateMachineHostBuilder builder, string host, string name)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new NamedEventProcessorFactory(host, name));

			return builder;
		}
	}
}