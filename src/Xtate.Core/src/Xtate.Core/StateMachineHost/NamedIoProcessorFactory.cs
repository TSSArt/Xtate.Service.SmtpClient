﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public sealed class NamedIoProcessorFactory : IIoProcessorFactory
	{
		private const int FreeSlotsCount = 2;

		private static readonly string HostName = GetHostName();

		private readonly string _host;
		private readonly string _name;

		public NamedIoProcessorFactory(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

			_name = name;
			_host = HostName;
		}

		public NamedIoProcessorFactory(string host, string name)
		{
			if (string.IsNullOrEmpty(host)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(host));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

			_host = host;
			_name = name;
		}

	#region Interface IIoProcessorFactory

		public async ValueTask<IIoProcessor> Create(IEventConsumer eventConsumer, CancellationToken token)
		{
			if (eventConsumer == null) throw new ArgumentNullException(nameof(eventConsumer));

			var processor = new NamedIoProcessor(eventConsumer, _host, _name);

			for (var i = 0; i < FreeSlotsCount; i ++)
			{
				processor.StartListener().Forget();
			}

			await processor.CheckPipeline(token).ConfigureAwait(false);

			return processor;
		}

	#endregion

		private static string GetHostName()
		{
			try
			{
				return Dns.GetHostName();
			}
			catch
			{
				return ".";
			}
		}
	}
}