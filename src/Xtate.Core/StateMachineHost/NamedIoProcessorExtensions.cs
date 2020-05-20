using System;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public static class NamedIoProcessorExtensions
	{
		public static StateMachineHostBuilder AddNamedIoProcessor(this StateMachineHostBuilder builder, string name)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddIoProcessorFactory(new NamedIoProcessorFactory(name));

			return builder;
		}

		public static StateMachineHostBuilder AddNamedIoProcessor(this StateMachineHostBuilder builder, string host, string name)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddIoProcessorFactory(new NamedIoProcessorFactory(host, name));

			return builder;
		}
	}
}