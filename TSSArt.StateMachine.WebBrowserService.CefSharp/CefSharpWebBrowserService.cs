using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		private string _url;
		private string _content;

		protected override async ValueTask<DataModelValue> Execute()
		{
			_url = Convert.ToString(Source, CultureInfo.InvariantCulture);
			_content = Convert.ToString(Content.ToObject(), CultureInfo.InvariantCulture);

			await Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			return DataModelValue.Undefined();
		}

		private async Task Show()
		{
			using var form = new BrowserForm(_url, _content);

			StopToken.Register(() => form.Close(DialogResult.Abort, DataModelValue.Undefined()));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			var @event = form.DialogResult == DialogResult.OK
					? new Event("browser.submit") { Data = form.Result }
					: new Event("browser.cancel");

			await ServiceCommunication.SendToCreator(@event).ConfigureAwait(false);
		}
	}
}