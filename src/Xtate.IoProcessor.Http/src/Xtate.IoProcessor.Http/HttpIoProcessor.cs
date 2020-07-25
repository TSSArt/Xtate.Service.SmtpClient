using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Xtate.IoProcessor
{
	[IoProcessor("http://www.w3.org/TR/scxml/#BasicHTTPEventProcessor", Alias = "http")]
	public sealed class HttpIoProcessor : IoProcessorBase, IAsyncDisposable
	{
		private const string MediaTypeTextPlain                 = "text/plain";
		private const string MediaTypeApplicationJson           = "application/json";
		private const string MediaTypeApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
		private const string EventNameParameterName             = "_scxmleventname";

		private static readonly ConcurrentDictionary<IPEndPoint, Host> Hosts      = new ConcurrentDictionary<IPEndPoint, Host>();
		private static readonly ImmutableArray<IPAddress>              Interfaces = GetInterfaces();

		private readonly Uri        _baseUri;
		private readonly PathString _path;
		private          IPEndPoint _ipEndPoint;

		public HttpIoProcessor(IEventConsumer eventConsumer, Uri baseUri, IPEndPoint ipEndPoint) : base(eventConsumer)
		{
			_baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
			_path = PathString.FromUriComponent(baseUri);
			_ipEndPoint = ipEndPoint;
		}

	#region Interface IAsyncDisposable

		public async ValueTask DisposeAsync()
		{
			if (Hosts.TryGetValue(_ipEndPoint, out var host))
			{
				await host.RemoveProcessor(this, token: default).ConfigureAwait(false);
			}
		}

	#endregion

		private static ImmutableArray<IPAddress> GetInterfaces()
		{
			var result = ImmutableArray.CreateBuilder<IPAddress>();

			foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
			{
				var ipInterfaceProperties = networkInterface.GetIPProperties();
				foreach (var ipInterfaceProperty in ipInterfaceProperties.UnicastAddresses)
				{
					result.Add(ipInterfaceProperty.Address);
				}
			}

			return result.ToImmutable();
		}

		public async ValueTask Start(CancellationToken token)
		{
			if (_ipEndPoint.Address.Equals(IPAddress.None) && _ipEndPoint.Port == 0)
			{
				_ipEndPoint = await FromUri(_baseUri).ConfigureAwait(false);
			}

			var host = Hosts.GetOrAdd(_ipEndPoint, Host.Create);

			await host.AddProcessor(this, token).ConfigureAwait(false);
		}

		private static async ValueTask<IPEndPoint> FromUri(Uri uri)
		{
			if (uri.IsLoopback)
			{
				return new IPEndPoint(IPAddress.Loopback, uri.Port);
			}

			var hostEntry = await Dns.GetHostEntryAsync(uri.DnsSafeHost).ConfigureAwait(false);

			IPAddress? listenAddress = null;

			foreach (var address in hostEntry.AddressList)
			{
				if (Interfaces.IndexOf(address) >= 0)
				{
					if (listenAddress != null)
					{
						throw new ProcessorException(Resources.Exception_Found_more_then_one_interface_to_listen);
					}

					listenAddress = address;
				}
			}

			if (listenAddress == null)
			{
				throw new ProcessorException(Resources.Exception_Can_t_match_network_interface_to_listen);
			}

			return new IPEndPoint(listenAddress, uri.Port);
		}

		protected override Uri GetTarget(SessionId sessionId)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));

			return new Uri(_baseUri, sessionId.Value);
		}

		protected override async ValueTask OutgoingEvent(SessionId sessionId, IOutgoingEvent evt, CancellationToken token)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			using var client = new HttpClient();

			if (evt.Target == null)
			{
				throw new ArgumentException(Resources.Exception_Target_is_not_defined, nameof(evt));
			}

			var targetUri = evt.Target.ToString();

			var content = GetContent(evt, out var eventNameInContent);
			if (evt.NameParts != null && !eventNameInContent)
			{
				targetUri = QueryHelpers.AddQueryString(targetUri, EventNameParameterName, EventName.ToName(evt.NameParts));
			}

			using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, targetUri)
										   {
												   Content = content,
												   Headers = { { @"Origin", GetTarget(sessionId).ToString() } }
										   };

			var httpResponseMessage = await client.SendAsync(httpRequestMessage, token).ConfigureAwait(false);
			httpResponseMessage.EnsureSuccessStatusCode();
		}

		private static HttpContent? GetContent(IOutgoingEvent evt, out bool eventNameInContent)
		{
			var data = evt.Data;
			var dataType = data.Type;

			switch (dataType)
			{
				case DataModelValueType.Undefined:
				case DataModelValueType.Null:
					eventNameInContent = evt.NameParts != null;

					return eventNameInContent ? new FormUrlEncodedContent(GetParameters(evt.NameParts, dataModelObject: null)) : null;

				case DataModelValueType.String:
					eventNameInContent = false;

					return new StringContent(data.AsString(), Encoding.UTF8, MediaTypeTextPlain);

				case DataModelValueType.Object:
				{
					var dataModelObject = data.AsObject();

					if (IsStringDictionary(dataModelObject))
					{
						eventNameInContent = true;
						return new FormUrlEncodedContent(GetParameters(evt.NameParts, dataModelObject));
					}

					eventNameInContent = false;

					return new StringContent(DataModelConverter.ToJson(data), Encoding.UTF8, MediaTypeApplicationJson);
				}

				case DataModelValueType.Array:
					eventNameInContent = false;

					return new StringContent(DataModelConverter.ToJson(data), Encoding.UTF8, MediaTypeApplicationJson);

				default:
					throw new NotSupportedException(Resources.Exception_Data_format_not_supported);
			}
		}

		private static bool IsStringDictionary(DataModelObject dataModelObject)
		{
			foreach (var pair in dataModelObject)
			{
				switch (pair.Value.Type)
				{
					case DataModelValueType.Object:
					case DataModelValueType.Array:
					case DataModelValueType.Number:
					case DataModelValueType.DateTime:
					case DataModelValueType.Boolean:
						return false;

					case DataModelValueType.Undefined:
					case DataModelValueType.Null:
					case DataModelValueType.String:
						break;

					default:
						Infrastructure.UnexpectedValue();
						break;
				}
			}

			return true;
		}

		private static IEnumerable<KeyValuePair<string, string>> GetParameters(ImmutableArray<IIdentifier> eventNameParts, DataModelObject? dataModelObject)
		{
			if (eventNameParts != null)
			{
				yield return new KeyValuePair<string, string>(EventNameParameterName, EventName.ToName(eventNameParts));
			}

			if (dataModelObject != null)
			{
				foreach (var pair in dataModelObject)
				{
					yield return new KeyValuePair<string, string>(pair.Key, Convert.ToString(pair.Value, CultureInfo.InvariantCulture));
				}
			}
		}

		private async ValueTask<bool> Handle(HttpRequest request)
		{
			SessionId sessionId;

			if (_path == @"/")
			{
				sessionId = ExtractSessionId(request.Path);
			}
			else if (request.Path.StartsWithSegments(_path, StringComparison.OrdinalIgnoreCase, out var sessionIdPathString))
			{
				sessionId = ExtractSessionId(sessionIdPathString);
			}
			else
			{
				return false;
			}

			var evt = await CreateEvent(request).ConfigureAwait(false);
			await IncomingEvent(sessionId, evt, token: default).ConfigureAwait(false);

			return true;
		}

		private static SessionId ExtractSessionId(PathString pathString)
		{
			var unescapedString = pathString.Value;

			if (unescapedString.Length > 0 && unescapedString[0] == '/')
			{
				return SessionId.FromString(unescapedString.Substring(1));
			}

			return SessionId.FromString(unescapedString);
		}

		private async ValueTask<IEvent> CreateEvent(HttpRequest request)
		{
			var contentType = request.ContentType != null ? new ContentType(request.ContentType) : new ContentType();
			var encoding = contentType.CharSet != null ? Encoding.GetEncoding(contentType.CharSet) : Encoding.ASCII;

			string body;
			using (var streamReader = new StreamReader(request.Body, encoding))
			{
				body = await streamReader.ReadToEndAsync().ConfigureAwait(false);
			}

			var origin = new Uri(request.Headers[@"Origin"].ToString());

			var data = CreateData(contentType.MediaType, body, out var eventNameInContent);

			var eventNameInQueryString = request.Query[EventNameParameterName];
			var eventName = !StringValues.IsNullOrEmpty(eventNameInQueryString) ? eventNameInQueryString[0] : null;

			eventName ??= eventNameInContent ?? request.Method;

			return new EventObject(eventName, origin, IoProcessorId, data);
		}

		private static DataModelValue CreateData(string mediaType, string body, out string? eventName)
		{
			eventName = null;

			if (mediaType == MediaTypeTextPlain)
			{
				return new DataModelValue(body);
			}

			if (mediaType == MediaTypeApplicationFormUrlEncoded)
			{
				var pairs = QueryHelpers.ParseQuery(body);
				var dataModelObject = new DataModelObject();

				foreach (var pair in pairs)
				{
					if (pair.Key == EventNameParameterName)
					{
						eventName = pair.Value[0];
					}
					else
					{
						foreach (var stringValue in pair.Value)
						{
							dataModelObject.Add(pair.Key, stringValue);
						}
					}
				}

				return new DataModelValue(dataModelObject);
			}

			if (mediaType == MediaTypeApplicationJson)
			{
				return DataModelConverter.FromJson(body);
			}

			return default;
		}

		private class Host
		{
			private readonly IWebHost                       _webHost;
			private          ImmutableList<HttpIoProcessor> _processors = ImmutableList<HttpIoProcessor>.Empty;

			private Host(IPEndPoint ipEndPoint)
			{
				_webHost = new WebHostBuilder()
						   .Configure(builder => builder.Run(HandleRequest))
						   .UseKestrel(ConfigureOptions)
						   .Build();

				void ConfigureOptions(KestrelServerOptions options)
				{
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

			public static Host Create(IPEndPoint ipEndPoint) => new Host(ipEndPoint);

			private async Task HandleRequest(HttpContext context)
			{
				foreach (var httpIoProcessor in _processors)
				{
					if (await httpIoProcessor.Handle(context.Request).ConfigureAwait(false))
					{
						return;
					}
				}
			}

			public async ValueTask AddProcessor(HttpIoProcessor httpIoProcessor, CancellationToken token)
			{
				ImmutableList<HttpIoProcessor> preVal, newVal;
				do
				{
					preVal = _processors;
					newVal = preVal.Add(httpIoProcessor);
				} while (Interlocked.CompareExchange(ref _processors, newVal, preVal) != preVal);

				if (preVal.Count == 0)
				{
					await _webHost.StartAsync(token).ConfigureAwait(false);
				}
			}

			public async ValueTask RemoveProcessor(HttpIoProcessor httpIoProcessor, CancellationToken token)
			{
				ImmutableList<HttpIoProcessor> preVal, newVal;
				do
				{
					preVal = _processors;
					newVal = preVal.Remove(httpIoProcessor);
				} while (Interlocked.CompareExchange(ref _processors, newVal, preVal) != preVal);

				if (newVal.Count == 0)
				{
					await _webHost.StopAsync(token).ConfigureAwait(false);
				}
			}
		}
	}
}