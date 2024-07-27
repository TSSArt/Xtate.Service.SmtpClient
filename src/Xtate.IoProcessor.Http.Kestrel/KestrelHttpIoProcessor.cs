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
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Xtate.Core;

namespace Xtate.IoProcessor;

internal sealed class KestrelHttpIoProcessor : HttpIoProcessorBase<KestrelHttpIoProcessorHost, HttpContext>
{
	private const string Id          = @"http://www.w3.org/TR/scxml/#BasicHTTPEventProcessor";
	private const string Alias       = @"http";
	private const string ErrorSuffix = @"Kestrel";

	public KestrelHttpIoProcessor(IEventConsumer eventConsumer, Uri baseUri, IPEndPoint ipEndPoint) : base(eventConsumer, baseUri, ipEndPoint, Id, Alias, ErrorSuffix) { }

	protected override KestrelHttpIoProcessorHost CreateHost(IPEndPoint ipEndPoint) => new(ipEndPoint);

	protected override string GetPath(HttpContext context) => context.Request.Path;

	protected override string? GetHeaderValue(HttpContext context, string name) => context.Request.Headers[name];

	protected override IPAddress? GetRemoteAddress(HttpContext context) => context.Connection.RemoteIpAddress;

	protected override string GetQueryString(HttpContext context) => context.Request.QueryString.Value;

	protected override Stream GetBody(HttpContext context) => context.Request.Body;

	protected override string GetMethod(HttpContext context) => context.Request.Method;
}