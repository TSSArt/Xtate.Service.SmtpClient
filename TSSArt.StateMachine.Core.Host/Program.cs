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
											   .AddHttpIoProcessor(baseUri)
											   .DisableVerboseValidation()
											   .AddResourceLoader(ResxResourceLoader.Instance)
											   .SetLogger(SerilogLogger.CreateLogger())
											   .Build();

			await stateMachineHost.StartHostAsync().ConfigureAwait(false);

			var name = Assembly.GetExecutingAssembly().GetName().Name;
			var autorun = stateMachineHost.ExecuteStateMachineAsync(new Uri($"resx://{name}/{name}/Scxml/autorun.scxml"));

			Application.Run();

			await stateMachineHost.StopHostAsync().ConfigureAwait(false);
			await autorun.ConfigureAwait(false);
		}
	}
}