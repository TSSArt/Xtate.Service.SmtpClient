using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TSSArt.StateMachine
{
	public sealed class HttpEventProcessorHandler : IDisposable
	{
		private readonly Uri                      _baseUri;
		private readonly List<HttpEventProcessor> _httpEventProcessors = new List<HttpEventProcessor>();
		private readonly ReaderWriterLockSlim     _rwLock              = new ReaderWriterLockSlim();

		public HttpEventProcessorHandler(Uri baseUri) => _baseUri = baseUri;

		public void Dispose()
		{
			_rwLock.Dispose();
		}

		public IEventProcessor CreateEventProcessor(string path = null)
		{
			_rwLock.EnterWriteLock();

			try
			{
				var httpEventProcessor = new HttpEventProcessor(_baseUri, path);
				_httpEventProcessors.Add(httpEventProcessor);
				return httpEventProcessor;
			}
			finally
			{
				_rwLock.ExitWriteLock();
			}
		}

		public async Task ProcessRequest(HttpContext context)
		{
			_rwLock.EnterReadLock();

			try
			{
				foreach (var httpEventProcessor in _httpEventProcessors)
				{
					if (await httpEventProcessor.Handle(context).ConfigureAwait(false))
					{
						return;
					}
				}
			}
			finally
			{
				_rwLock.ExitReadLock();
			}
		}
	}
}