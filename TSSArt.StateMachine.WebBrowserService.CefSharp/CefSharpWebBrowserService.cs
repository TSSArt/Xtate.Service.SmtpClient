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
			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, result: default));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			var result = new DataModelObject();

			if (form.DialogResult == DialogResult.OK)
			{
				result["status"] = new DataModelValue("ok");

				var parameters = new DataModelObject();
				foreach (var pair in form.Result)
				{
					parameters[pair.Key] = new DataModelValue(pair.Value);
				}

				parameters.Freeze();

				result["parameters"] = new DataModelValue(parameters);
				result.Freeze();
			}
			else
			{
				result["status"] = new DataModelValue("cancel");
			}

			result.Freeze();

			return new DataModelValue(result);
		}
	}
}