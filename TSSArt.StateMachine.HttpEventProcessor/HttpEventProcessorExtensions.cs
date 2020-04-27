using System;
using System.Net;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public static class HttpEventProcessorExtensions
	{
		public static StateMachineHostBuilder AddHttpEventProcessor(this StateMachineHostBuilder builder, Uri baseUri)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new HttpEventProcessorFactory(baseUri, new IPEndPoint(IPAddress.None, port: 0)));

			return builder;
		}

		public static StateMachineHostBuilder AddHttpEventProcessor(this StateMachineHostBuilder builder, Uri baseUri, IPEndPoint ipEndPoint)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new HttpEventProcessorFactory(baseUri, ipEndPoint));

			return builder;
		}
	}
}