using System;
using System.Net;

namespace TSSArt.StateMachine
{
	public static class HttpEventProcessorExtensions
	{
		public static IoProcessorBuilder AddHttpEventProcessor(this IoProcessorBuilder builder, Uri baseUri)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new HttpEventProcessorFactory(baseUri, new IPEndPoint(IPAddress.None, port: 0)));

			return builder;
		}
		
		public static IoProcessorBuilder AddHttpEventProcessor(this IoProcessorBuilder builder, Uri baseUri, IPEndPoint ipEndPoint)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddEventProcessorFactory(new HttpEventProcessorFactory(baseUri, ipEndPoint));

			return builder;
		}
	}
}