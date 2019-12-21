using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using TSSArt.StateMachine.EcmaScript;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine.IntegrationTest
{
	internal class Program
	{
		private static async Task Main(string[] args)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());

			var baseUri = new Uri(args.Length > 0 ? args[0] : "http://localhost:5001/");
			using var handler = new HttpEventProcessorHandler(baseUri);
			var httpEventProcessor = handler.CreateEventProcessor(baseUri.AbsolutePath);
			var options = new IoProcessorOptions
						  {
								  EventProcessors = new[] { httpEventProcessor },
								  ServiceFactories = new[] { HttpClientService.Factory, SmtpClientService.Factory },
								  DataModelHandlerFactories = new[] { EcmaScriptDataModelHandler.Factory },
								  CustomActionProviders = new[] { BasicCustomActionProvider.Instance, MimeCustomActionProvider.Instance, MidCustomActionProvider.Instance }
						  };

			await using var ioProcessor = new IoProcessor(options);

			var task = ioProcessor.Execute(sessionId: "test-tssart-com-sign-up", GetStateMachine("TSSArt.StateMachine.IntegrationTest.test-tssart-com-sign-up.xml"));

			var webHost = new WebHostBuilder()
						  .Configure(builder => builder.Run(handler.ProcessRequest))
						  .UseKestrel(serverOptions => serverOptions.ListenAnyIP(baseUri.Port))
						  .Build();

			await webHost.StartAsync().ConfigureAwait(false);

			await task.ConfigureAwait(false);

			await webHost.StopAsync().ConfigureAwait(false);
		}

		private static IStateMachine GetStateMachine(string name)
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
			using var xmlReader = XmlReader.Create(stream);
			return new ScxmlDirector(xmlReader, new BuilderFactory()).ConstructStateMachine();
		}
	}
}