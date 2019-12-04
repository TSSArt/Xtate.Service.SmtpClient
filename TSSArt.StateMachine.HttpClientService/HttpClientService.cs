using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#HTTPClient", Alias = "http")]
	public class HttpClientService : SimpleServiceBase
	{
		private const string MediaTypeApplicationJson = " application/json";

		public static readonly IServiceFactory Factory = SimpleServiceFactory<HttpClientService>.Instance;

		private static readonly FieldInfo DomainTableField = typeof(CookieContainer).GetField(name: "m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo ListField        = typeof(CookieContainer).Assembly.GetType("System.Net.PathList").GetField(name: "m_list", BindingFlags.Instance | BindingFlags.NonPublic);

		private static string GetString(DataModelValue val, string key, string defaultValue = null)
		{
			var dataModelValue = val.AsObject()[key];

			return dataModelValue.Type != DataModelValueType.Undefined ? dataModelValue.AsString() : defaultValue;
		}

		private static bool GetBoolean(DataModelValue val, string key, bool defaultValue = false)
		{
			var dataModelValue = val.AsObject()[key];

			return dataModelValue.Type != DataModelValueType.Undefined ? dataModelValue.AsBoolean() : defaultValue;
		}

		private static DateTime GetDateTime(DataModelValue val, string key, DateTime defaultValue = default)
		{
			var dataModelValue = val.AsObject()[key];

			return dataModelValue.Type != DataModelValueType.Undefined ? dataModelValue.AsDateTime() : defaultValue;
		}

		private static IList<DataModelValue> GetArray(DataModelValue val, string key)
		{
			var dataModelValue = val.AsObject()[key];

			return dataModelValue.Type != DataModelValueType.Undefined ? (IList<DataModelValue>) dataModelValue.AsArray() : Array.Empty<DataModelValue>();
		}

		private static DataModelObject GetObject(DataModelValue val, string key)
		{
			var dataModelValue = val.AsObject()[key];

			return dataModelValue.Type != DataModelValueType.Undefined ? dataModelValue.AsObject() : null;
		}

		protected override async ValueTask<DataModelValue> Execute()
		{
			var request = WebRequest.CreateHttp(Source);

			request.AllowAutoRedirect = GetBoolean(Parameters, key: "autoRedirect", defaultValue: true);
			request.Method = GetString(Parameters, key: "method", defaultValue: "get");

			var headers = GetArray(Parameters, key: "headers");

			foreach (var header in headers)
			{
				request.Headers.Add(GetString(header, key: "name"), GetString(header, key: "value"));
			}

			var accept = GetString(Parameters, key: "accept");
			if (accept != null)
			{
				request.Accept = accept;
			}

			var cookieContainer = CreateCookieContainer(GetArray(Parameters, key: "cookies"));
			request.CookieContainer = cookieContainer;

			var result = new DataModelObject();

			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse) await request.GetResponseAsync().ConfigureAwait(false);
			}
			catch (WebException ex)
			{
				response = (HttpWebResponse) ex.Response;

				result["webExceptionStatus"] = new DataModelValue(ex.Status.ToString());
			}

			result["statusCode"] = new DataModelValue((int) response.StatusCode);
			result["statusDescription"] = new DataModelValue(response.StatusDescription);

			var contentType = new ContentType(response.ContentType);
			if (contentType.MediaType == MediaTypeApplicationJson)
			{
				using var jsonDocument = JsonDocument.Parse(response.GetResponseStream());

				result["content"] = GetDataModelValue(jsonDocument.RootElement);
			}
			else
			{
				var responseStream = response.GetResponseStream();
				if (responseStream != null)
				{
					var htmlDocument = new HtmlDocument();
					htmlDocument.Load(responseStream);

					result["capture"] = CaptureData(htmlDocument);
				}
			}

			result["headers"] = new DataModelValue(GetHeaders(response.Headers));
			result["cookies"] = new DataModelValue(GetCookies(cookieContainer));

			return new DataModelValue(result);
		}

		private DataModelValue GetDataModelValue(in JsonElement element)
		{
			return element.ValueKind switch
			{
					JsonValueKind.Undefined => DataModelValue.Undefined(),
					JsonValueKind.Object => new DataModelValue(GetDataModelObject(element.EnumerateObject())),
					JsonValueKind.Array => new DataModelValue(GetDataModeArray(element.EnumerateArray())),
					JsonValueKind.String => new DataModelValue(element.GetString()),
					JsonValueKind.Number => new DataModelValue(element.GetDouble()),
					JsonValueKind.True => new DataModelValue(true),
					JsonValueKind.False => new DataModelValue(false),
					JsonValueKind.Null => DataModelValue.Null(),
					_ => throw new ArgumentOutOfRangeException()
			};
		}

		private DataModelObject GetDataModelObject(JsonElement.ObjectEnumerator enumerateObject)
		{
			var obj = new DataModelObject();

			foreach (var prop in enumerateObject)
			{
				obj[prop.Name] = GetDataModelValue(prop.Value);
			}

			return obj;
		}

		private DataModelArray GetDataModeArray(JsonElement.ArrayEnumerator enumerateArray)
		{
			var arr = new DataModelArray();

			foreach (var prop in enumerateArray)
			{
				arr.Add(GetDataModelValue(prop));
			}

			return arr;
		}

		private DataModelValue CaptureData(HtmlDocument htmlDocument)
		{
			var obj = new DataModelObject();

			var capture = GetObject(Parameters, key: "capture");
			if (capture != null)
			{
				foreach (var name in capture.Properties)
				{
					var val = capture[name];
					var xpath = GetString(val, key: "xpath");
					var attr = GetString(val, key: "attr");
					var regex = GetString(val, key: "regex");
					var result = CaptureEntry(htmlDocument, xpath, attr, regex);

					if (result.Type != DataModelValueType.Undefined)
					{
						obj[name] = result;
					}
				}
			}

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
				return DataModelValue.Undefined();
			}

			var text = attr != null ? node.GetAttributeValue(attr, def: null) : node.InnerHtml;

			if (text == null)
			{
				return DataModelValue.Undefined();
			}

			if (pattern == null)
			{
				return new DataModelValue(text);
			}

			var regex = new Regex(pattern);
			var match = regex.Match(text);

			if (!match.Success)
			{
				return DataModelValue.Undefined();
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

			return new DataModelValue(obj);
		}

		private static DataModelArray GetHeaders(WebHeaderCollection headers)
		{
			var arr = new DataModelArray();

			foreach (var name in headers.AllKeys)
			{
				arr.Add(new DataModelValue(new DataModelObject
										   {
												   ["name"] = new DataModelValue(name),
												   ["value"] = new DataModelValue(headers[name])
										   }));
			}

			return arr;
		}

		private static CookieContainer CreateCookieContainer(IEnumerable<DataModelValue> cookies)
		{
			var container = new CookieContainer();

			foreach (var cookie in cookies)
			{
				container.Add(new Cookie
							  {
									  Name = GetString(cookie, key: "name"),
									  Value = GetString(cookie, key: "value"),
									  Path = GetString(cookie, key: "path", defaultValue: "/"),
									  Domain = GetString(cookie, key: "domain"),
									  Expires = GetDateTime(cookie, key: "expires"),
									  HttpOnly = GetBoolean(cookie, key: "httpOnly"),
									  Port = GetString(cookie, key: "port"),
									  Secure = GetBoolean(cookie, key: "secure")
							  });
			}

			return container;
		}

		private static DataModelArray GetCookies(CookieContainer container)
		{
			var allCookies = from object pathList in ((Hashtable) DomainTableField.GetValue(container)).Values
							 from IEnumerable cookies in ((SortedList) ListField.GetValue(pathList)).Values
							 from Cookie cookie in cookies
							 select cookie;

			var arr = new DataModelArray();

			foreach (var cookie in allCookies)
			{
				arr.Add(new DataModelValue(new DataModelObject
										   {
												   ["name"] = new DataModelValue(cookie.Name),
												   ["value"] = new DataModelValue(cookie.Value),
												   ["path"] = new DataModelValue(cookie.Path),
												   ["domain"] = new DataModelValue(cookie.Domain),
												   ["expires"] = new DataModelValue(cookie.Expires),
												   ["httpOnly"] = new DataModelValue(cookie.HttpOnly),
												   ["port"] = new DataModelValue(cookie.Port),
												   ["secure"] = new DataModelValue(cookie.Secure)
										   }));
			}

			return arr;
		}
	}
}