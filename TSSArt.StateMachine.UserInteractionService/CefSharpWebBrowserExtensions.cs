using System;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine
{
	public static class UserInteractionExtensions
	{
		public static IoProcessorOptionsBuilder AddUserInteraction(this IoProcessorOptionsBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddServiceFactory(InputService.Factory);

			return builder;
		}
	}
}