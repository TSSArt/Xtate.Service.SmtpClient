using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Core.Host
{
	internal static class Program
	{
		[STAThread]
		public static async Task Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var baseUri = new Uri(args.Length > 0 ? args[0] : "http://localhost:5000/");

			await using var stateMachineHost = new StateMachineHostBuilder()
										  .AddEcmaScript()
										  .AddCefSharpWebBrowser()
										  .AddUserInteraction()
										  .AddHttpEventProcessor(baseUri)
										  .DisableVerboseValidation()
										  .AddResourceLoader(ResxResourceLoader.Instance)
										  .Build();

			await stateMachineHost.StartAsync().ConfigureAwait(false);

			var name = Assembly.GetExecutingAssembly().GetName().Name;
			var autorun = stateMachineHost.Execute(new Uri($"resx://{name}/{name}/Scxml/autorun.scxml"));

			Application.Run();

			await stateMachineHost.StopAsync().ConfigureAwait(false);
			await autorun.ConfigureAwait(false);
		}
	}
}