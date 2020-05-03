using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TSSArt.StateMachine.EcmaScript;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine.IntegrationTest
{
	internal static class Program
	{
		private static readonly Uri ScxmlBase = new Uri("res://TSSArt.StateMachine.IntegrationTest/TSSArt.StateMachine.IntegrationTest/Scxml/");

		private static async Task Main(string[] args)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());

			await using var stateMachineHost = new StateMachineHostBuilder()
											   .AddEcmaScript()
											   .AddHttpIoProcessor(new Uri(args.Length > 0 ? args[0] : "http://localhost:5001/"))
											   .AddServiceFactory(HttpClientService.Factory)
											   .AddServiceFactory(SmtpClientService.Factory)
											   .AddCustomActionFactory(BasicCustomActionFactory.Instance)
											   .AddCustomActionFactory(MimeCustomActionFactory.Instance)
											   .AddCustomActionFactory(MidCustomActionFactory.Instance)
											   .AddResourceLoader(ResxResourceLoader.Instance)
											   .SetConfigurationValue(key: "uiEndpoint", value: "http://localhost:5000/dialog")
											   .SetConfigurationValue(key: "mailEndpoint", value: "http://mid.dev.tssart.com/MailServer/Web2/api/Mail/")
											   .Build();

			await stateMachineHost.StartAsync().ConfigureAwait(false);

			var prms = new
					   {
							   loginUrl = "https://test.tssart.com/wp-login.php",
							   username = "tadex1",
							   password = "123456"
					   };

			var task = stateMachineHost.ExecuteAsync(new Uri(ScxmlBase, relativeUri: "signup.scxml"), DataModelValue.FromObject(prms));

			dynamic result = await task.ConfigureAwait(false);

			var prms2 = new { profileUrl = "https://test.tssart.com/wp-admin/profile.php", result.data.cookies };
			var task2 = stateMachineHost.ExecuteAsync(new Uri(ScxmlBase, relativeUri: "captureEmail.scxml"), DataModelValue.FromObject(prms2));

			dynamic _ = await task2.ConfigureAwait(false);

			await stateMachineHost.StopAsync().ConfigureAwait(false);
		}
	}
}