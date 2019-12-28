using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		private string _content;
		private string _url;

		protected override ValueTask<ServiceResult> Execute()
		{
			_url = Source?.ToString();
			_content = RawContent ?? Content.AsStringOrDefault();

			var task = Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			return new ValueTask<ServiceResult>(task);
		}

		private ServiceResult Show()
		{
			using var form = new BrowserForm(_url, _content);
			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, DataModelValue.Undefined));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			if (form.DialogResult == DialogResult.OK)
			{
				return new ServiceResult { DoneEventData = form.Result, DoneEventSuffix = "ok" };
			}

			return new ServiceResult { DoneEventSuffix = "cancel" };
		}
	}
}