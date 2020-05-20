using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows.Forms;
using CefSharp;
using CefSharp.Handler;
using CefSharp.Internals;
using CefSharp.WinForms;
using Microsoft.AspNetCore.WebUtilities;

namespace Xtate.Services
{
	public partial class BrowserForm : Form
	{
		public BrowserForm(Uri? url, string? content)
		{
			InitializeComponent();

			Controls.Add(new ChromiumWebBrowser(url?.ToString())
						 {
								 Dock = DockStyle.Fill,
								 RequestHandler = new CustomRequestHandler(this, url, content)
						 });
		}

		public IDictionary<string, string>? Result { get; private set; }

		public void Close(DialogResult dialogResult, IDictionary<string, string>? result)
		{
			Result = result;
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

		private readonly string?     _content;
		private readonly BrowserForm _form;
		private readonly Uri?        _url;

		public CustomRequestHandler(BrowserForm form, Uri? url, string? content)
		{
			_form = form;
			_url = url;
			_content = content;
		}

		[SuppressMessage(category: "ReSharper", checkId: "SuspiciousTypeConversion.Global", Justification = "Uri can be compared with string")]
		protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request,
																			 bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));

			if (_url != null && _url.Equals(request.Url))
			{
				if (request.Method == "GET")
				{
					return new InMemoryResourceRequestHandler(Encoding.ASCII.GetBytes(_content ?? string.Empty), mimeType: null);
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

					_form.Close(DialogResult.OK, result);

					return EmptyHtmlHandler;
				}
			}

			return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
		}
	}
}