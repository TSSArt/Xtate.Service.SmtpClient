using System;
using Serilog;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public static class LoggerSerilogExtensions
	{
		public static StateMachineHostBuilder SetSerilogLogger(this StateMachineHostBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var configuration = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console();

			builder.SetLogger(new SerilogLogger(configuration));

			return builder;
		}

		public static StateMachineHostBuilder SetSerilogLogger(this StateMachineHostBuilder builder, Action<LoggerConfiguration> options)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (options == null) throw new ArgumentNullException(nameof(options));

			var configuration = new LoggerConfiguration();

			options(configuration);

			builder.SetLogger(new SerilogLogger(configuration));

			return builder;
		}
	}
}