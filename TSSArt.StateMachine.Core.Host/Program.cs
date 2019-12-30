using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using TSSArt.StateMachine.EcmaScript;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine.Core.Host
{
	internal static class Program
	{
		[STAThread]
		public static async Task Main(string[] args)
		{
#if NETCOREAPP3_0
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var baseUri = new Uri(args.Length > 0 ? args[0] : "http://localhost:5000/");
			using var handler = new HttpEventProcessorHandler(baseUri);
			var httpEventProcessor = handler.CreateEventProcessor(baseUri.AbsolutePath);
			var options = new IoProcessorOptions
						  {
								  EventProcessors = new[] { httpEventProcessor },
								  ServiceFactories = new[] { WebBrowserService.GetFactory<CefSharpWebBrowserService>(), InputService.Factory },
								  DataModelHandlerFactories = new[] { EcmaScriptDataModelHandler.Factory }
						  };

			await using var ioProcessor = new IoProcessor(options);

			var prefix = Assembly.GetExecutingAssembly().GetName().Name + ".Autorun.";
			const string suffix = ".xml";
			foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
			{
				if (name.StartsWith(prefix) && name.EndsWith(suffix))
				{
					var sessionId = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
					var task = ioProcessor.Execute(sessionId, GetStateMachine(name));
				}
			}

			var webHost = new WebHostBuilder()
						  .Configure(builder => builder.Run(handler.ProcessRequest))
						  .UseKestrel(serverOptions => serverOptions.ListenAnyIP(baseUri.Port))
						  .Build();

			await webHost.StartAsync().ConfigureAwait(false);

			Application.Run();

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