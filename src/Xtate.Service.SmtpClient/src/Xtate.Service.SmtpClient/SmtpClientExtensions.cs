using System;
using Xtate.Services;

namespace Xtate.EcmaScript
{
	public static class SmtpClientExtensions
	{
		public static StateMachineHostBuilder AddSmtpClient(this StateMachineHostBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddServiceFactory(SmtpClientService.Factory);

			return builder;
		}
	}
}