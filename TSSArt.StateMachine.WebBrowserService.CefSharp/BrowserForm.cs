using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
		private readonly string                     _source;
		private readonly string                     _type;
		private readonly Dictionary<string, string> _vars;

		public BrowserForm(string source, string type, Dictionary<string, string> vars)
		{
			_source = source;
			_type = type;
			_vars = vars;

			InitializeComponent();

			Controls.Add(new ChromiumWebBrowser(source)
						 {
								 Dock = DockStyle.Fill,
								 RequestHandler = new CustomRequestHandler(this)
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

		private class CustomRequestHandler : RequestHandler
		{
			private static readonly IResourceRequestHandler EmptyHtmlHandler = new InMemoryResourceRequestHandler(Encoding.ASCII.GetBytes("<html/>"), mimeType: null);

			private readonly BrowserForm _parent;

			public CustomRequestHandler(BrowserForm parent) => _parent = parent;

			protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request,
																				 bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
			{
				if (request.Url == _parent._source)
				{
					if (request.Method == "GET")
					{
						using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.Services.Form." + _parent._type + ".htm");

						if (stream == null)
						{
							_parent.Close(DialogResult.Abort, new DataModelValue("Type " + _parent._type + " does not supported"));

							return EmptyHtmlHandler;
						}

						var reader = new StreamReader(stream, Encoding.ASCII);
						var template = reader.ReadToEnd();

						var dataString = Regex.Replace(template, pattern: @"\<\#(\w+)\#\>", match => _parent._vars[match.Groups[1].Value]);

						return new InMemoryResourceRequestHandler(Encoding.ASCII.GetBytes(dataString), mimeType: null);
					}

					if (request.Method == "POST")
					{
						var postData = Encoding.ASCII.GetString(request.PostData.Elements[0].Bytes);
						var pairs = QueryHelpers.ParseQuery(postData);

						var result = new DataModelObject();
						foreach (var pair in pairs)
						{
							result[pair.Key] = new DataModelValue(pair.Value.ToString());
						}

						_parent.Close(DialogResult.OK, new DataModelValue(result));

						return EmptyHtmlHandler;
					}
				}

				return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
			}
		}
	}
}