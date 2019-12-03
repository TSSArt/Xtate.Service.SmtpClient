using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace TSSArt.StateMachine
{
	[EventProcessor("http://www.w3.org/TR/scxml/#BasicHTTPEventProcessor", Alias = "http")]
	public sealed class HttpEventProcessor : EventProcessorBase
	{
		private const string MediaTypeTextPlain                 = "text/plain";
		private const string MediaTypeApplicationJson           = "application/json";
		private const string MediaTypeApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
		private const string EventNameParameterName             = "_scxmleventname";

		private readonly Uri    _baseUri;
		private readonly string _path;

		public HttpEventProcessor(Uri baseUri, string path)
		{
			_baseUri = new Uri(baseUri, path);
			_path = path;
		}

		protected override Uri GetTarget(string sessionId) => new Uri(_baseUri, sessionId);

		protected override async ValueTask OutgoingEvent(string sessionId, IOutgoingEvent @event, CancellationToken token)
		{
			if (@event == null) throw new ArgumentNullException(nameof(@event));

			using var client = new HttpClient();

			var targetUri = @event.Target.ToString();

			if (@event.NameParts != null)
			{
				targetUri = QueryHelpers.AddQueryString(targetUri, EventNameParameterName, EventName.ToName(@event.NameParts));
			}

			var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, targetUri)
									 {
											 Content = GetContent(@event),
											 Headers = { { "Origin", GetTarget(sessionId).ToString() } }
									 };

			var httpResponseMessage = await client.SendAsync(httpRequestMessage, token).ConfigureAwait(false);
			httpResponseMessage.EnsureSuccessStatusCode();
		}

		private static HttpContent GetContent(IOutgoingEvent @event)
		{
			var data = @event.Data;
			if (data.Type == DataModelValueType.Undefined || data.Type == DataModelValueType.Null)
			{
				return null;
			}

			if (data.Type == DataModelValueType.String)
			{
				return new StringContent(data.AsString(), Encoding.UTF8, MediaTypeTextPlain);
			}

			if (data.Type == DataModelValueType.Object)
			{
				var dataModelObject = data.AsObject();

				if (IsAllValuesAreSimple(dataModelObject))
				{
					return new FormUrlEncodedContent(GetParameters(dataModelObject));
				}

				return new StringContent(dataModelObject.ToString(format: "JSON", CultureInfo.InvariantCulture), Encoding.UTF8, MediaTypeApplicationJson);
			}

			if (data.Type == DataModelValueType.Array)
			{
				return new StringContent(data.AsArray().ToString(format: "JSON", CultureInfo.InvariantCulture), Encoding.UTF8, MediaTypeApplicationJson);
			}

			throw new NotSupportedException();
		}

		private static bool IsAllValuesAreSimple(DataModelObject dataModelObject)
		{
			foreach (var name in dataModelObject.Properties)
			{
				switch (dataModelObject[name].Type)
				{
					case DataModelValueType.Object:
					case DataModelValueType.Array:
						return false;

					case DataModelValueType.Undefined:
					case DataModelValueType.Null:
					case DataModelValueType.String:
					case DataModelValueType.Number:
					case DataModelValueType.DateTime:
					case DataModelValueType.Boolean:
						break;

					default: throw new ArgumentOutOfRangeException();
				}
			}

			return true;
		}

		private static IEnumerable<KeyValuePair<string, string>> GetParameters(DataModelObject dataModelObject)
		{
			foreach (var name in dataModelObject.Properties)
			{
				var value = dataModelObject[name].ToObject();
				if (value != null)
				{
					yield return new KeyValuePair<string, string>(name, Convert.ToString(value, CultureInfo.InvariantCulture));
				}
			}
		}

		public async ValueTask<bool> Handle(HttpRequest request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));

			string sessionId;

			if (_path == null)
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

			var @event = await CreateEvent(request).ConfigureAwait(false);
			await IncomingEvent(sessionId, @event, token: default).ConfigureAwait(false);

			return true;
		}

		private async ValueTask<IEvent> CreateEvent(HttpRequest request)
		{
			var eventName = request.Query[EventNameParameterName].ToString();

			if (string.IsNullOrEmpty(eventName))
			{
				eventName = request.Method;
			}

			var contentType = request.ContentType != null ? new ContentType(request.ContentType) : new ContentType();
			var encoding = contentType.CharSet != null ? Encoding.GetEncoding(contentType.CharSet) : Encoding.ASCII;

			string body;
			using (var streamReader = new StreamReader(request.Body, encoding))
			{
				body = await streamReader.ReadToEndAsync().ConfigureAwait(false);
			}

			var origin = new Uri(request.Headers["Origin"].ToString());

			return new EventObject(EventType.External, sendId: null, EventName.ToParts(eventName), invokeId: null, origin, EventProcessorId, CreateData(contentType.MediaType, body));
		}

		private DataModelValue CreateData(string mediaType, string body)
		{
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
					dataModelObject[pair.Key] = new DataModelValue(pair.Value.ToString());
				}

				return new DataModelValue(dataModelObject);
			}

			if (mediaType == MediaTypeApplicationJson)
			{
				var jsonDocument = JsonDocument.Parse(body);
				return Map(jsonDocument.RootElement);
			}

			return default;
		}

		private DataModelValue Map(JsonElement element)
		{
			return element.ValueKind switch
			{
					JsonValueKind.Undefined => DataModelValue.Undefined(),
					JsonValueKind.Object => new DataModelValue(MapObject(element)),
					JsonValueKind.Array => new DataModelValue(MapArray(element)),
					JsonValueKind.String => new DataModelValue(element.GetString()),
					JsonValueKind.Number => new DataModelValue(element.GetDouble()),
					JsonValueKind.True => new DataModelValue(true),
					JsonValueKind.False => new DataModelValue(false),
					JsonValueKind.Null => DataModelValue.Null(),
					_ => throw new ArgumentOutOfRangeException()
			};
		}

		private DataModelObject MapObject(JsonElement element)
		{
			var dataModelObject = new DataModelObject();

			foreach (var jsonProperty in element.EnumerateObject())
			{
				dataModelObject[jsonProperty.Name] = Map(jsonProperty.Value);
			}

			return dataModelObject;
		}

		private DataModelArray MapArray(JsonElement element)
		{
			var dataModelArray = new DataModelArray();

			foreach (var jsonElement in element.EnumerateArray())
			{
				dataModelArray.Add(Map(jsonElement));
			}

			return dataModelArray;
		}
	}
}