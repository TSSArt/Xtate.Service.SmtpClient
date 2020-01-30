using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
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

			var cookies = parameters["cookies"].AsArrayOrEmpty().Select(CreateCookie);

			var capturesObj = parameters["capture"].AsObjectOrEmpty();
			var captures = from name in capturesObj.Properties
						   let capture = capturesObj[name].AsObjectOrEmpty()
						   select new Capture
								  {
										  Name = name,
										  XPaths = GetArray(capture["xpath"]),
										  Attributes = GetArray(capture["attr"]),
										  Regex = capture["regex"].AsStringOrDefault()
								  };

			static string[] GetArray(DataModelValue val)
			{
				if (val.Type == DataModelValueType.Array)
				{
					return val.AsArray().Select(p => p.AsStringOrDefault()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
				}

				var str = val.AsStringOrDefault();

				return !string.IsNullOrEmpty(str) ? new[] { str } : null;
			}

			var response = await DoRequest(Source, method, accept, autoRedirect, contentType, headers, cookies, captures.ToArray(), Content, StopToken).ConfigureAwait(false);

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

		private static Cookie CreateCookie(DataModelValue val)
		{
			var cookieObj = val.AsObjectOrEmpty();
			var cookie = new Cookie
						 {
								 Name = cookieObj["name"].AsStringOrDefault(),
								 Value = cookieObj["value"].AsStringOrDefault(),
								 Path = cookieObj["path"].AsStringOrDefault(),
								 Domain = cookieObj["domain"].AsStringOrDefault(),
								 Expires = cookieObj["expires"].AsDateTimeOrDefault() ?? DateTime.MinValue,
								 HttpOnly = cookieObj["httpOnly"].AsBooleanOrDefault() ?? false,
								 Secure = cookieObj["secure"].AsBooleanOrDefault() ?? false
						 };

			var port = cookieObj["port"].AsStringOrDefault();

			if (!string.IsNullOrEmpty(port))
			{
				cookie.Port = port;
			}

			return cookie;
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

		private static HttpContent CreateJsonContent(DataModelValue content) => new ByteArrayContent(DataModelConverter.ToJsonUtf8Bytes(content));

		private static HttpContent CreateDefaultContent(DataModelValue content) => new StringContent(content.ToObject()?.ToString() ?? string.Empty, Encoding.UTF8);

		private static ValueTask<DataModelValue> FromJsonContent(Stream stream, CancellationToken token) => DataModelConverter.FromJsonAsync(stream, token);

		private static ValueTask<DataModelValue> FromHtmlContent(Stream stream, IEnumerable<Capture> captures)
		{
			var htmlDocument = new HtmlDocument();
			htmlDocument.Load(stream);

			return new ValueTask<DataModelValue>(CaptureData(htmlDocument, captures));
		}

		private static async ValueTask<Response> DoRequest(Uri requestUri, string method, string accept, bool autoRedirect, string contentType,
														   IEnumerable<KeyValuePair<string, string>> headers, IEnumerable<Cookie> cookies,
														   Capture[] captures, DataModelValue content, CancellationToken token)
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
				result.Content = await FromHtmlContent(stream, captures).ConfigureAwait(false);
			}

			result.Headers = from key in response.Headers.AllKeys select new KeyValuePair<string, string>(key, response.Headers[key]);

			AppendCookies(requestUri, cookieContainer, response);

			result.Cookies = from object pathList in ((Hashtable) DomainTableField.GetValue(cookieContainer)).Values
							 from IEnumerable cookieList in ((SortedList) ListField.GetValue(pathList)).Values
							 from Cookie cookie in cookieList
							 select cookie;

			return result;
		}

		private static void AppendCookies(Uri uri, CookieContainer cookieContainer, HttpWebResponse response)
		{
			var list = response.Headers.GetValues("Set-Cookie");

			if (list == null)
			{
				return;
			}

			var uriBuilder = new UriBuilder(uri);

			foreach (var header in list)
			{
				foreach (var path in GetPaths(header))
				{
					uriBuilder.Path = path;

					cookieContainer.SetCookies(uriBuilder.Uri, header);
				}
			}

			IEnumerable<string> GetPaths(string header)
			{
				const string pathPrefix = "path=";

				var p1 = 0;
				while ((p1 = header.IndexOf(pathPrefix, p1, StringComparison.Ordinal)) >= 0)
				{
					p1 += pathPrefix.Length;

					var p2 = header.IndexOf(value: ';', p1);

					var path = p2 < 0 ? header.Substring(p1) : header.Substring(p1, p2 - p1);

					yield return path.Trim().Trim('\"');
				}
			}
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

		private static DataModelValue CaptureData(HtmlDocument htmlDocument, IEnumerable<Capture> captures)
		{
			var obj = new DataModelObject();

			foreach (var capture in captures)
			{
				var result = CaptureEntry(htmlDocument, capture.XPaths, capture.Attributes, capture.Regex);

				if (result.Type != DataModelValueType.Undefined)
				{
					obj[capture.Name] = result;
				}
			}

			obj.Freeze();

			return new DataModelValue(obj);
		}

		private static DataModelValue CaptureEntry(HtmlDocument htmlDocument, string[] xpaths, string[] attrs, string pattern)
		{
			if (xpaths == null)
			{
				return CaptureInNode(htmlDocument.DocumentNode, attrs, pattern);
			}

			var array = new DataModelArray();

			foreach (var xpath in xpaths)
			{
				var nodes = htmlDocument.DocumentNode.SelectNodes(xpath);

				if (nodes == null)
				{
					continue;
				}

				foreach (var node in nodes)
				{
					var result = CaptureInNode(node, attrs, pattern);

					if (result.Type != DataModelValueType.Undefined)
					{
						array.Add(result);
					}
				}
			}

			array.Freeze();

			return new DataModelValue(array);
		}

		private static DataModelValue CaptureInNode(HtmlNode node, string[] attrs, string pattern)
		{
			if (attrs == null)
			{
				return CaptureInText(node.InnerHtml, pattern);
			}

			var obj = new DataModelObject();

			foreach (var attr in attrs)
			{
				var value = attr.StartsWith(value: "::", StringComparison.Ordinal) ? GetSpecialAttributeValue(node, attr) : node.GetAttributeValue(attr, def: null);

				if (value == null)
				{
					return DataModelValue.Undefined;
				}

				obj[attr] = CaptureInText(value, pattern);
			}

			obj.Freeze();

			return new DataModelValue(obj);
		}

		private static string GetSpecialAttributeValue(HtmlNode node, string attr) =>
				attr switch
				{
						"::value" => GetHtmlValue(node),
						_ => null
				};

		private static string GetHtmlValue(HtmlNode node) =>
				node.Name switch
				{
						"input" => GetInputValue(node),	
						"textarea" => GetInputValue(node),
						"select" => GetSelectValue(node),
						_ => null
				};

		private static string GetSelectValue(HtmlNode node)
		{
			var selected = node.ChildNodes.FirstOrDefault(n => n.Name == "option" && n.Attributes.Contains("selected"))
						   ?? node.ChildNodes.FirstOrDefault(n => n.Name == "option");

			return selected != null ? GetValue(selected, check: false) : null;
		}

		private static string GetInputValue(HtmlNode node) =>
				node.GetAttributeValue(name: "type", def: null) switch
				{
						"radio" => GetValue(node, check: true),
						"checkbox" => GetValue(node, check: true),
						_ => GetValue(node, check: false)
				};

		private static string GetValue(HtmlNode node, bool check)
		{
			if (check && !node.Attributes.Contains("checked"))
			{
				return null;
			}

			return node.GetAttributeValue(name: "value", def: null) ?? node.InnerText;
		}

		private static DataModelValue CaptureInText(string text, string pattern)
		{
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
			public string   Name       { get; set; }
			public string[] XPaths     { get; set; }
			public string[] Attributes { get; set; }
			public string   Regex      { get; set; }
		}
	}
}