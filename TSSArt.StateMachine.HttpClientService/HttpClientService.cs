using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#HTTPClient", Alias = "http")]
	public class HttpClientService : SimpleServiceBase
	{
		private const string MediaTypeApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
		private const string MediaTypeApplicationJson           = "application/json";
		private const string MediaTypeTextHtml                  = "text/html";

		public static readonly IServiceFactory Factory = SimpleServiceFactory<HttpClientService>.Instance;

		private static readonly FieldInfo DomainTableField = typeof(CookieContainer).GetField(name: "m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo ListField        = typeof(CookieContainer).Assembly.GetType("System.Net.PathList").GetField(name: "m_list", BindingFlags.Instance | BindingFlags.NonPublic);

		protected override async ValueTask<DataModelValue> Execute()
		{
			var parameters = Parameters.AsObjectOrEmpty();

			var method = parameters["method"].AsStringOrDefault() ?? "get";
			var autoRedirect = parameters["autoRedirect"].AsBooleanOrDefault() ?? true;
			var accept = parameters["accept"].AsStringOrDefault();
			var contentType = parameters["contentType"].AsStringOrDefault();

			var headers = from obj in parameters["headers"].AsArrayOrEmpty()
						  let name = obj.AsObjectOrEmpty()["name"].AsStringOrDefault()
						  let value = obj.AsObjectOrEmpty()["value"].AsStringOrDefault()
						  where !string.IsNullOrEmpty(name) && value != null
						  select new KeyValuePair<string, string>(name, value);

			var cookies = from obj in parameters["cookies"].AsArrayOrEmpty()
						  let cookie = obj.AsObjectOrEmpty()
						  select new Cookie
								 {
										 Name = cookie["name"].AsStringOrDefault(),
										 Value = cookie["value"].AsStringOrDefault(),
										 Path = cookie["path"].AsStringOrDefault(),
										 Domain = cookie["domain"].AsStringOrDefault(),
										 Expires = cookie["expires"].AsDateTimeOrDefault() ?? DateTime.MinValue,
										 HttpOnly = cookie["httpOnly"].AsBooleanOrDefault() ?? false,
										 Port = cookie["port"].AsStringOrDefault(),
										 Secure = cookie["secure"].AsBooleanOrDefault() ?? false
								 };

			var capturesObj = parameters["capture"].AsObjectOrEmpty();
			var captures = from name in capturesObj.Properties
						   let capture = capturesObj[name].AsObjectOrEmpty()
						   select new Capture
								  {
										  Name = name,
										  Attribute = capture["attr"].AsStringOrDefault(),
										  XPath = capture["xpath"].AsStringOrDefault(),
										  Regex = capture["regex"].AsStringOrDefault()
								  };

			var response = await DoRequest(Source, method, accept, autoRedirect, contentType, headers, cookies, captures, Content, StopToken).ConfigureAwait(false);

			var responseHeaders = new DataModelArray();
			foreach (var header in response.Headers)
			{
				var pair = new DataModelObject { ["name"] = new DataModelValue(header.Key), ["value"] = new DataModelValue(header.Value) };
				pair.Freeze();
				responseHeaders.Add(new DataModelValue(pair));
			}

			responseHeaders.Freeze();

			var responseCookies = new DataModelArray();
			foreach (var cookie in response.Cookies)
			{
				var cookieObj = new DataModelObject
								{
										["name"] = new DataModelValue(cookie.Name),
										["value"] = new DataModelValue(cookie.Value),
										["path"] = new DataModelValue(cookie.Path),
										["domain"] = new DataModelValue(cookie.Domain),
										["httpOnly"] = new DataModelValue(cookie.HttpOnly),
										["port"] = new DataModelValue(cookie.Port),
										["secure"] = new DataModelValue(cookie.Secure)
								};

				if (cookie.Expires != default)
				{
					cookieObj["expires"] = new DataModelValue(cookie.Expires);
				}

				cookieObj.Freeze();
				responseCookies.Add(new DataModelValue(cookieObj));
			}

			responseCookies.Freeze();

			var result = new DataModelObject
						 {
								 ["statusCode"] = new DataModelValue(response.StatusCode),
								 ["statusDescription"] = new DataModelValue(response.StatusDescription),
								 ["webExceptionStatus"] = new DataModelValue(response.WebExceptionStatus),
								 ["headers"] = new DataModelValue(responseHeaders),
								 ["cookies"] = new DataModelValue(responseCookies),
								 ["content"] = response.Content
						 };
			result.Freeze();

			return new DataModelValue(result);
		}

		private static HttpContent CreateFormUrlEncodedContent(DataModelValue content)
		{
			var forms = from p in content.AsArrayOrEmpty()
						let name = p.AsObjectOrEmpty()["name"].AsStringOrDefault()
						let value = p.AsObjectOrEmpty()["value"].AsStringOrDefault()
						where !string.IsNullOrEmpty(name) && value != null
						select new KeyValuePair<string, string>(name, value);

			return new FormUrlEncodedContent(forms);
		}

		private static HttpContent CreateJsonContent(DataModelValue content) => new StringContent(content.ToString(format: "JSON", CultureInfo.InvariantCulture), Encoding.ASCII);

		private static HttpContent CreateDefaultContent(DataModelValue content) => new StringContent(content.ToObject()?.ToString() ?? string.Empty, Encoding.UTF8);

		private static async ValueTask<DataModelValue> FromJsonContent(Stream stream, CancellationToken token)
		{
			using var jsonDocument = await JsonDocument.ParseAsync(stream, options: default, token).ConfigureAwait(false);

			return GetDataModelValue(jsonDocument.RootElement);
		}

		private static ValueTask<DataModelValue> FromTextHtmlContent(Stream stream, IEnumerable<Capture> captures)
		{
			var htmlDocument = new HtmlDocument();
			htmlDocument.Load(stream);

			return new ValueTask<DataModelValue>(CaptureData(htmlDocument, captures));
		}

		private static async ValueTask<Response> DoRequest(Uri requestUri, string method, string accept, bool autoRedirect, string contentType,
														   IEnumerable<KeyValuePair<string, string>> headers, IEnumerable<Cookie> cookies,
														   IEnumerable<Capture> captures, DataModelValue content, CancellationToken token)
		{
			var request = WebRequest.CreateHttp(requestUri);

			request.Method = method;
			request.AllowAutoRedirect = autoRedirect;

			if (accept != null)
			{
				request.Accept = accept;
			}

			foreach (var header in headers)
			{
				request.Headers.Add(header.Key, header.Value);
			}

			var cookieContainer = new CookieContainer();

			foreach (var cookie in cookies)
			{
				cookieContainer.Add(cookie);
			}

			request.CookieContainer = cookieContainer;

			await WriteContent(request, contentType, content).ConfigureAwait(false);

			var result = new Response();

			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse) await request.GetResponseAsync().ConfigureAwait(false);
			}
			catch (WebException ex)
			{
				response = (HttpWebResponse) ex.Response;

				result.WebExceptionStatus = ex.Status.ToString();
			}

			result.StatusCode = (int) response.StatusCode;
			result.StatusDescription = response.StatusDescription;

#if NETSTANDARD2_1
			await using var stream = response.GetResponseStream();
#else
			using var stream = response.GetResponseStream();
#endif
			var responseContentType = new ContentType(response.ContentType);

			if (responseContentType.MediaType == MediaTypeApplicationJson)
			{
				result.Content = await FromJsonContent(stream, token).ConfigureAwait(false);
			}
			else if (responseContentType.MediaType == MediaTypeTextHtml)
			{
				result.Content = await FromTextHtmlContent(stream, captures).ConfigureAwait(false);
			}

			result.Headers = from key in response.Headers.AllKeys select new KeyValuePair<string, string>(key, response.Headers[key]);

			result.Cookies = from object pathList in ((Hashtable) DomainTableField.GetValue(cookieContainer)).Values
							 from IEnumerable cookieList in ((SortedList) ListField.GetValue(pathList)).Values
							 from Cookie cookie in cookieList
							 select cookie;

			return result;
		}

		private static async ValueTask WriteContent(WebRequest request, string contentType, DataModelValue content)
		{
			if (contentType == null)
			{
				return;
			}

			request.ContentType = contentType;

#if NETSTANDARD2_1
			await using var stream = request.GetRequestStream();
#else
			using var stream = request.GetRequestStream();
#endif

			if (contentType == MediaTypeApplicationFormUrlEncoded)
			{
				using var httpContent = CreateFormUrlEncodedContent(content);
				await httpContent.CopyToAsync(stream).ConfigureAwait(false);
			}
			else if (contentType == MediaTypeApplicationJson)
			{
				using var httpContent = CreateJsonContent(content);
				await httpContent.CopyToAsync(stream).ConfigureAwait(false);
			}
			else
			{
				using var httpContent = CreateDefaultContent(content);
				await httpContent.CopyToAsync(stream).ConfigureAwait(false);
			}
		}

		private static DataModelValue GetDataModelValue(in JsonElement element)
		{
			return element.ValueKind switch
			{
					JsonValueKind.Undefined => DataModelValue.Undefined,
					JsonValueKind.Object => new DataModelValue(GetDataModelObject(element.EnumerateObject())),
					JsonValueKind.Array => new DataModelValue(GetDataModeArray(element.EnumerateArray())),
					JsonValueKind.String => new DataModelValue(element.GetString()),
					JsonValueKind.Number => new DataModelValue(element.GetDouble()),
					JsonValueKind.True => new DataModelValue(true),
					JsonValueKind.False => new DataModelValue(false),
					JsonValueKind.Null => DataModelValue.Null,
					_ => throw new ArgumentOutOfRangeException()
			};
		}

		private static DataModelObject GetDataModelObject(JsonElement.ObjectEnumerator enumerateObject)
		{
			var obj = new DataModelObject();

			foreach (var prop in enumerateObject)
			{
				obj[prop.Name] = GetDataModelValue(prop.Value);
			}

			obj.Freeze();

			return obj;
		}

		private static DataModelArray GetDataModeArray(JsonElement.ArrayEnumerator enumerateArray)
		{
			var arr = new DataModelArray();

			foreach (var prop in enumerateArray)
			{
				arr.Add(GetDataModelValue(prop));
			}

			arr.Freeze();

			return arr;
		}

		private static DataModelValue CaptureData(HtmlDocument htmlDocument, IEnumerable<Capture> captures)
		{
			var obj = new DataModelObject();

			foreach (var capture in captures)
			{
				var result = CaptureEntry(htmlDocument, capture.XPath, capture.Attribute, capture.Regex);

				if (result.Type != DataModelValueType.Undefined)
				{
					obj[capture.Name] = result;
				}
			}

			obj.Freeze();

			return new DataModelValue(obj);
		}

		private static DataModelValue CaptureEntry(HtmlDocument htmlDocument, string xpath, string attr, string pattern)
		{
			var node = htmlDocument.DocumentNode;

			if (xpath != null)
			{
				node = htmlDocument.DocumentNode.SelectSingleNode(xpath);
			}

			if (node == null)
			{
				return DataModelValue.Undefined;
			}

			var text = attr != null ? node.GetAttributeValue(attr, def: null) : node.InnerHtml;

			if (text == null)
			{
				return DataModelValue.Undefined;
			}

			if (pattern == null)
			{
				return new DataModelValue(text);
			}

			var regex = new Regex(pattern);
			var match = regex.Match(text);

			if (!match.Success)
			{
				return DataModelValue.Undefined;
			}

			if (match.Groups.Count == 1)
			{
				return new DataModelValue(match.Groups[0].Value);
			}

			var obj = new DataModelObject();
			foreach (var name in regex.GetGroupNames())
			{
				obj[name] = new DataModelValue(match.Groups[name].Value);
			}

			obj.Freeze();

			return new DataModelValue(obj);
		}

		private struct Response
		{
			public int                                       StatusCode         { get; set; }
			public string                                    StatusDescription  { get; set; }
			public string                                    WebExceptionStatus { get; set; }
			public DataModelValue                            Content            { get; set; }
			public IEnumerable<KeyValuePair<string, string>> Headers            { get; set; }
			public IEnumerable<Cookie>                       Cookies            { get; set; }
		}

		private struct Capture
		{
			public string Name      { get; set; }
			public string XPath     { get; set; }
			public string Attribute { get; set; }
			public string Regex     { get; set; }
		}
	}
}