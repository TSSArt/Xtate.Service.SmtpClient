using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using TSSArt.StateMachine.Properties;

namespace TSSArt.StateMachine
{
	[EventProcessor("http://www.w3.org/TR/scxml/#BasicHTTPEventProcessor", Alias = "http")]
	public sealed class HttpEventProcessor : EventProcessorBase, IAsyncDisposable
	{
		private const string MediaTypeTextPlain                 = "text/plain";
		private const string MediaTypeApplicationJson           = "application/json";
		private const string MediaTypeApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
		private const string EventNameParameterName             = "_scxmleventname";

		private readonly Uri             _baseUri;
		private readonly Func<ValueTask> _onDispose;
		private readonly string          _path;

		public HttpEventProcessor(IEventConsumer eventConsumer, Uri baseUri, string path, Func<ValueTask> onDispose) : base(eventConsumer)
		{
			if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
			if (string.IsNullOrEmpty(path)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(path));

			_baseUri = new Uri(baseUri, path);
			_path = path;
			_onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
		}

	#region Interface IAsyncDisposable

		public ValueTask DisposeAsync() => _onDispose();

	#endregion

		protected override Uri GetTarget(string sessionId) => new Uri(_baseUri, sessionId);

		protected override async ValueTask OutgoingEvent(string sessionId, IOutgoingEvent evt, CancellationToken token)
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

			if (dataType == DataModelValueType.Undefined || dataType == DataModelValueType.Null)
			{
				eventNameInContent = evt.NameParts != null;
				return eventNameInContent ? new FormUrlEncodedContent(GetParameters(evt.NameParts, dataModelObject: null)) : null;
			}

			if (dataType == DataModelValueType.String)
			{
				eventNameInContent = false;
				return new StringContent(data.AsString(), Encoding.UTF8, MediaTypeTextPlain);
			}

			if (dataType == DataModelValueType.Object)
			{
				var dataModelObject = data.AsObject()!;

				if (IsStringDictionary(dataModelObject))
				{
					eventNameInContent = true;
					return new FormUrlEncodedContent(GetParameters(evt.NameParts, dataModelObject));
				}

				eventNameInContent = false;
				return new StringContent(DataModelConverter.ToJson(data), Encoding.UTF8, MediaTypeApplicationJson);
			}

			if (dataType == DataModelValueType.Array)
			{
				eventNameInContent = false;
				return new StringContent(DataModelConverter.ToJson(data), Encoding.UTF8, MediaTypeApplicationJson);
			}

			throw new NotSupportedException(Resources.Exception_Data_format_not_supported);
		}

		private static bool IsStringDictionary(DataModelObject dataModelObject)
		{
			foreach (var name in dataModelObject.Properties)
			{
				switch (dataModelObject[name].Type)
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
				foreach (var name in dataModelObject.Properties)
				{
					var value = dataModelObject[name].ToObject();
					yield return new KeyValuePair<string, string>(name, Convert.ToString(value, CultureInfo.InvariantCulture));
				}
			}
		}

		public async ValueTask<bool> Handle(HttpRequest request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));

			string sessionId;

			if (_path == @"/")
			{
				sessionId = Path.GetFileName(request.Path);
			}
			else if (request.Path.StartsWithSegments(_path, out var sessionIdPathString))
			{
				sessionId = Path.GetFileName(sessionIdPathString);
			}
			else
			{
				return false;
			}

			var evt = await CreateEvent(request).ConfigureAwait(false);
			await IncomingEvent(sessionId, evt, token: default).ConfigureAwait(false);

			return true;
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

			return new EventObject(eventName, origin, EventProcessorId, data);
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
						dataModelObject[pair.Key] = new DataModelValue(pair.Value.ToString());
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
	}
}