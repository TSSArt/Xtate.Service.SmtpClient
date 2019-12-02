using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		private readonly TaskCompletionSource<DataModelValue> _tcs = new TaskCompletionSource<DataModelValue>();

		protected override async ValueTask<DataModelValue> Execute()
		{
			using var form = new BrowserForm(Source.ToString());

			form.Closed += (sender, args) => _tcs.SetResult(new DataModelValue(55));

			Application.OpenForms[0].Invoke(new Action(() => { form.Show(); }));

			return await _tcs.Task.ConfigureAwait(false);
		}
	}
}