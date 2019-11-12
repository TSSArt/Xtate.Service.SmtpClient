using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("https://www.w3.org/Protocols/HTTP/", Alias = "http")]
	public class HttpClientService : SimpleServiceBase
	{
		public static readonly IServiceFactory Factory = SimpleServiceFactory<HttpClientService>.Instance;

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
			result["statusCodeText"] = new DataModelValue(response.StatusCode.ToString());

			var responseStream = response.GetResponseStream();
			if (responseStream != null)
			{
				var memory = new MemoryStream();
				await responseStream.CopyToAsync(memory).ConfigureAwait(false);

				var content = Encoding.UTF8.GetString(memory.ToArray());
				result["content"] = new DataModelValue(content);
			}

			result["headers"] = new DataModelValue(GetHeaders(response.Headers));
			result["cookies"] = new DataModelValue(GetCookies(cookieContainer));

			return new DataModelValue(result);
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

		private static readonly FieldInfo DomainTableField = typeof(CookieContainer).GetField(name: "m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo ListField        = typeof(CookieContainer).Assembly.GetType("System.Net.PathList").GetField(name: "m_list", BindingFlags.Instance | BindingFlags.NonPublic);

		private DataModelArray GetCookies(CookieContainer container)
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