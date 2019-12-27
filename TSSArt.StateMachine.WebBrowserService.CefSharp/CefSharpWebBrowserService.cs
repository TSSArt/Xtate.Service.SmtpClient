using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		private string _content;
		private string _url;

		protected override ValueTask<DataModelValue> Execute()
		{
			_url = Source?.ToString();
			_content = RawContent ?? Content.AsStringOrDefault();

			var task = Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			return new ValueTask<DataModelValue>(task);
		}

		private DataModelValue Show()
		{
			using var form = new BrowserForm(_url, _content);
			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, DataModelValue.Undefined));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			if (form.DialogResult == DialogResult.OK)
			{
				return form.Result;
			}

			throw new OperationCanceledException();
		}
	}
}