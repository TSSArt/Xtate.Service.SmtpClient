using System;
using System.Text;
using System.Windows.Forms;
using CefSharp;
using CefSharp.Handler;
using CefSharp.Internals;
using CefSharp.WinForms;
using Microsoft.AspNetCore.WebUtilities;

namespace TSSArt.StateMachine.Services
{
	public partial class BrowserForm : Form
	{
		public BrowserForm(string url, string content)
		{
			InitializeComponent();

			Controls.Add(new ChromiumWebBrowser(url)
						 {
								 Dock = DockStyle.Fill,
								 RequestHandler = new CustomRequestHandler(this, url, content)
						 });
		}

		public DataModelValue Result { get; private set; }

		public void Close(DialogResult dialogResult, DataModelValue result)
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
		private readonly        string                  _content;

		private readonly BrowserForm _form;
		private readonly string      _url;

		public CustomRequestHandler(BrowserForm form, string url, string content)
		{
			_form = form;
			_url = url;
			_content = content;
		}

		protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request,
																			 bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));

			if (request.Url == _url)
			{
				if (request.Method == "GET")
				{
					return new InMemoryResourceRequestHandler(Encoding.ASCII.GetBytes(_content), mimeType: null);
				}

				if (request.Method == "POST")
				{
					var postData = Encoding.ASCII.GetString(request.PostData.Elements[0].Bytes);

					var result = new DataModelObject();
					foreach (var pair in QueryHelpers.ParseQuery(postData))
					{
						result[pair.Key] = new DataModelValue(pair.Value.ToString());
					}

					_form.Close(DialogResult.OK, new DataModelValue(result));

					return EmptyHtmlHandler;
				}
			}

			return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
		}
	}
}