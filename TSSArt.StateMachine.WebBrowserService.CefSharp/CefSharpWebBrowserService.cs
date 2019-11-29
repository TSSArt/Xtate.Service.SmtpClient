using System.Threading.Tasks;

namespace TSSArt.StateMachine.Services
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		private readonly TaskCompletionSource<DataModelValue> _tcs = new TaskCompletionSource<DataModelValue>();

		protected override async ValueTask<DataModelValue> Execute()
		{
			using var form = new BrowserForm("http://google.com/");

			form.Closed += (sender, args) => _tcs.SetResult(new DataModelValue(55));

			form.Show();

			return await _tcs.Task.ConfigureAwait(false);
		}
	}
}