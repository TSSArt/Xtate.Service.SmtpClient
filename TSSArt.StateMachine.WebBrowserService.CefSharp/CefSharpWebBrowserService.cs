using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		protected override ValueTask<DataModelValue> Execute()
		{
			var task = Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			return new ValueTask<DataModelValue>(task);
		}

		[SuppressMessage(category: "ReSharper", checkId: "AccessToDisposedClosure", Justification = "Form closed by external event")]
		private DataModelValue Show()
		{
			var url = Source?.ToString();
			var content = RawContent ?? Content.AsStringOrDefault();

			using var form = new BrowserForm(url != null ? new Uri(url) : null, content);

			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, result: default));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			var result = new DataModelObject();

			if (form.DialogResult == DialogResult.OK)
			{
				result["status"] = "ok";

				var parameters = new DataModelObject();
				if (form.Result != null)
				{
					foreach (var pair in form.Result)
					{
						parameters[pair.Key] = pair.Value;
					}
				}

				result["parameters"] = parameters;
			}
			else
			{
				result["status"] = "cancel";
			}

			return result;
		}
	}
}