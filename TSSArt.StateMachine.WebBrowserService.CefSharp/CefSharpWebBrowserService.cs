using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		private string _source;
		private string _type;

		private readonly Dictionary<string, string> _vars = new Dictionary<string, string>();

		protected override async ValueTask<DataModelValue> Execute()
		{
			var parameters = Parameters.AsObject();
			_source = parameters["source"].AsString();
			_type = parameters["type"].AsString();

			var varsValue = parameters["vars"];
			if (varsValue.Type == DataModelValueType.Object)
			{
				var vars = varsValue.AsObject();
				foreach (var name in vars.Properties)
				{
					var val = vars[name];
					if (val.Type == DataModelValueType.String)
					{
						_vars[name] = val.AsString();
					}
				}
			}

			await Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			return DataModelValue.Undefined();
		}

		private async Task Show()
		{
			using var form = new BrowserForm(_source, _type, _vars);

			StopToken.Register(() => form.Close(DialogResult.Abort, DataModelValue.Undefined()));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			var @event = form.DialogResult == DialogResult.OK ? new Event("browser.submit") { Data = form.Result } : new Event("browser.cancel");

			await ServiceCommunication.SendToCreator(@event).ConfigureAwait(false);
		}
	}
}