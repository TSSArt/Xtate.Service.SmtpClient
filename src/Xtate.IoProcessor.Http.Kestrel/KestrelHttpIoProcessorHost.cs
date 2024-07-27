// Copyright © 2019-2024 Sergii Artemenko
// 
// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Xtate.IoProcessor;

internal sealed class KestrelHttpIoProcessorHost : HttpIoProcessorHostBase<KestrelHttpIoProcessorHost, HttpContext>, IDisposable, IAsyncDisposable
{
	private readonly IWebHost _webHost;

	public KestrelHttpIoProcessorHost(IPEndPoint ipEndPoint)
	{
		_webHost = new WebHostBuilder()
				   .Configure(builder => builder.Run(HandleRequest))
				   .UseKestrel(ConfigureOptions)
				   .Build();

		void ConfigureOptions(KestrelServerOptions options)
		{
			options.AllowSynchronousIO = false;

			if (ipEndPoint.Address.Equals(IPAddress.Any) || ipEndPoint.Address.Equals(IPAddress.IPv6Any))
			{
				options.ListenAnyIP(ipEndPoint.Port);
			}
			else if (IPAddress.IsLoopback(ipEndPoint.Address))
			{
				options.ListenLocalhost(ipEndPoint.Port);
			}
			else
			{
				options.Listen(ipEndPoint);
			}
		}
	}

#region Interface IAsyncDisposable

	public ValueTask DisposeAsync()
	{
		_webHost.Dispose();

		return default;
	}

#endregion

#region Interface IDisposable

	public void Dispose() => _webHost.Dispose();

#endregion

	private async Task HandleRequest(HttpContext context)
	{
		foreach (var httpIoProcessor in Processors)
		{
			if (await httpIoProcessor.Handle(context, context.RequestAborted).ConfigureAwait(false))
			{
				context.Response.StatusCode = (int) HttpStatusCode.NoContent;

				return;
			}
		}

		context.Response.StatusCode = (int) HttpStatusCode.NotFound;
	}

	protected override ValueTask StartHost(CancellationToken token) => new(_webHost.StartAsync(token));

	protected override ValueTask StopHost(CancellationToken token) => new(_webHost.StopAsync(token));
}