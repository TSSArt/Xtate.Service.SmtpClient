using System;
using System.Net;
using Xtate.Annotations;
using Xtate.IoProcessor;

namespace Xtate
{
	[PublicAPI]
	public static class HttpIoProcessorExtensions
	{
		public static StateMachineHostBuilder AddHttpIoProcessor(this StateMachineHostBuilder builder, Uri baseUri)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddIoProcessorFactory(new HttpIoProcessorFactory(baseUri, new IPEndPoint(IPAddress.None, port: 0)));

			return builder;
		}

		public static StateMachineHostBuilder AddHttpIoProcessor(this StateMachineHostBuilder builder, Uri baseUri, IPEndPoint ipEndPoint)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddIoProcessorFactory(new HttpIoProcessorFactory(baseUri, ipEndPoint));

			return builder;
		}
	}
}