using System;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine
{
	public static class UserInteractionExtensions
	{
		public static IoProcessorBuilder AddUserInteraction(this IoProcessorBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddServiceFactory(InputService.Factory);

			return builder;
		}
	}
}