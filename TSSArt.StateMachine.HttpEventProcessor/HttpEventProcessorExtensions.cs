using System;

namespace TSSArt.StateMachine
{
	public static class HttpEventProcessorExtensions
	{
		public static IoProcessorOptionsBuilder AddHttpEventProcessor(this IoProcessorOptionsBuilder builder, Uri baseUri, string path)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new HttpEventProcessorFactory(baseUri, path));

			return builder;
		}
	}
}