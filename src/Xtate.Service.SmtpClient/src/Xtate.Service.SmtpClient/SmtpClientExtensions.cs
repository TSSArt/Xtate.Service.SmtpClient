using System;
using Xtate.Service;

namespace Xtate
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