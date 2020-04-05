using System;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public static class NamedEventProcessorExtensions
	{
		public static IoProcessorOptionsBuilder AddNamedEventProcessor(this IoProcessorOptionsBuilder builder, string name)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new NamedEventProcessorFactory(name));

			return builder;
		}

		public static IoProcessorOptionsBuilder AddNamedEventProcessor(this IoProcessorOptionsBuilder builder, string host, string name)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new NamedEventProcessorFactory(host, name));

			return builder;
		}
	}
}