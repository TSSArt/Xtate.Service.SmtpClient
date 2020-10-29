#region Copyright © 2019-2020 Sergii Artemenko

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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows.Forms;
using CefSharp;
using CefSharp.Handler;
using CefSharp.Internals;
using CefSharp.WinForms;
using Microsoft.AspNetCore.WebUtilities;
using Cookie = CefSharp.Cookie;
using NetCookie = System.Net.Cookie;

namespace Xtate.Service
{
	public partial class BrowserForm : Form
	{
		public BrowserForm(Uri? url, string? document, CookieCollection? cookieCollection)
		{
			InitializeComponent();

			Controls.Add(new ChromiumWebBrowser(url?.ToString())
						 {
								 Dock = DockStyle.Fill,
								 RequestHandler = new CustomRequestHandler(this, url, document)
						 });

			if (cookieCollection is not null)
			{
				var cookieManager = Cef.GetGlobalCookieManager();

				Debug.Assert(cookieCollection != null);

				foreach (NetCookie cookie in cookieCollection)
				{
					var cefCookie = GetCefCookie(cookie);

					cookieManager.SetCookieAsync(url: null, cefCookie).Wait();
				}
			}
		}

		public IDictionary<string, string>? Result { get; private set; }

		public CookieCollection? Cookies { get; private set; }

		private static Cookie GetCefCookie(NetCookie cookie) =>
				new Cookie
				{
						Domain = cookie.Domain,
						Path = cookie.Path,
						Name = cookie.Name,
						Value = cookie.Value,
						HttpOnly = cookie.HttpOnly,
						Expires = cookie.Expires,
						Secure = cookie.Secure
				};

		public void Close(DialogResult dialogResult, IDictionary<string, string>? result, CookieCollection? cookies)
		{
			Result = result;
			Cookies = cookies;
			DialogResult = dialogResult;

			if (InvokeRequired)
			{
				BeginInvoke(new Action(Close));
			}
			else
			{
				Close();
			}
		}
	}

	public class CustomRequestHandler : RequestHandler
	{
		private static readonly IResourceRequestHandler EmptyHtmlHandler = new InMemoryResourceRequestHandler(Encoding.ASCII.GetBytes("<html/>"), mimeType: null);

		private readonly string?     _document;
		private readonly BrowserForm _form;
		private readonly Uri?        _url;

		public CustomRequestHandler(BrowserForm form, Uri? url, string? document)
		{
			_form = form;
			_url = url;
			_document = document;
		}

		protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request,
																			 bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			if (_url is not null && _url.Equals(request.Url))
			{
				if (request.Method == "GET")
				{
					return new InMemoryResourceRequestHandler(Encoding.ASCII.GetBytes(_document ?? string.Empty), mimeType: null);
				}

				if (request.Method == "POST")
				{
					var postData = Encoding.ASCII.GetString(request.PostData.Elements[0].Bytes);

					var result = new Dictionary<string, string>();
					var parameters = QueryHelpers.ParseNullableQuery(postData);
					foreach (var pair in parameters)
					{
						result[pair.Key] = pair.Value[0];
					}

					var cookieManager = Cef.GetGlobalCookieManager();
					using var cookieVisitor = new CookieVisitor();
					cookieManager.VisitAllCookies(cookieVisitor);

					_form.Close(DialogResult.OK, result, cookieVisitor.CookieCollection);

					return EmptyHtmlHandler;
				}
			}

			return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
		}

		private class CookieVisitor : ICookieVisitor
		{
			public CookieCollection CookieCollection { get; } = new CookieCollection();

		#region Interface ICookieVisitor

			public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
			{
				CookieCollection.Add(new NetCookie
									 {
											 Domain = cookie.Domain,
											 Path = cookie.Path,
											 Name = cookie.Name,
											 Value = cookie.Value,
											 HttpOnly = cookie.HttpOnly,
											 Expires = cookie.Expires ?? DateTime.MinValue,
											 Secure = cookie.Secure
									 });

				return true;
			}

		#endregion

		#region Interface IDisposable

			public void Dispose() { }

		#endregion
		}
	}
}