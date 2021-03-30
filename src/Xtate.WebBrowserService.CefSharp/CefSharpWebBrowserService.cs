#region Copyright © 2019-2021 Sergii Artemenko

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

#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xtate.Service
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		protected override ValueTask<DataModelValue> Execute()
		{
			var task = Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			return new ValueTask<DataModelValue>(task);
		}

		[SuppressMessage(category: "ReSharper", checkId: "AccessToDisposedClosure")]
		private DataModelValue Show()
		{
			var url = Source?.ToString();
			var document = RawContent ?? Content.AsStringOrDefault() ?? GetParameter("document").AsStringOrDefault();

			if (string.IsNullOrEmpty(document))
			{
				Infra.Fail("Content can't be empty");
			}

			using var form = new BrowserForm(url is not null ? new Uri(url) : null, document, GetCookies());

			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, result: default, cookies: default));

			form.Closed += (_, _) => Application.ExitThread();

			Application.Run(form);

			var result = new DataModelList();

			if (form.DialogResult == DialogResult.OK)
			{
				result.Add(key: "status", value: "ok");

				if (form.Result is not null)
				{
					var parameters = new DataModelList();

					foreach (var pair in form.Result)
					{
						parameters.Add(pair.Key, pair.Value);
					}

					result.Add(key: "parameters", parameters);
				}
				else
				{
					result.Add(key: "parameters", DataModelList.Empty);
				}

				if (form.Cookies is not null)
				{
					var cookiesArray = new DataModelList();

					foreach (Cookie cookie in form.Cookies)
					{
						cookiesArray.Add(new DataModelList
										 {
											 ["domain"] = cookie.Domain,
											 ["path"] = cookie.Path,
											 ["name"] = cookie.Name,
											 ["value"] = cookie.Value,
											 ["httpOnly"] = cookie.HttpOnly,
											 ["secure"] = cookie.Secure,
											 ["expires"] = cookie.Expires
										 });
					}

					result.Add(key: "cookies", cookiesArray);
				}
				else
				{
					result.Add(key: "cookies", DataModelList.Empty);
				}
			}
			else
			{
				result.Add(key: "status", value: "cancel");
			}

			return result;
		}

		private CookieCollection? GetCookies()
		{
			var cookies = GetParameter("cookies").AsListOrEmpty();

			if (cookies.Count == 0)
			{
				return null;
			}

			var result = new CookieCollection();

			foreach (var value in cookies.Values)
			{
				var list = value.AsListOrEmpty();

				result.Add(new Cookie
						   {
							   Domain = list["domain"].AsStringOrDefault(),
							   Path = list["path"].AsStringOrDefault(),
							   Name = list["name"].AsStringOrDefault(),
							   Value = list["value"].AsStringOrDefault(),
							   HttpOnly = list["httpOnly"].AsBooleanOrDefault() ?? false,
							   Secure = list["secure"].AsBooleanOrDefault() ?? false,
							   Expires = list["expires"].AsDateTimeOrDefault()?.ToDateTime() ?? DateTime.MinValue
						   });
			}

			return result;
		}

		private DataModelValue GetParameter(string name)
		{
			var list = Parameters.AsListOrDefault() ?? Content.AsListOrEmpty();

			if (list.TryGet(name, list.CaseInsensitive, out var entry))
			{
				return entry.Value;
			}

			return default;
		}
	}
}