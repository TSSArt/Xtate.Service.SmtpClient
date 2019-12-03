using System;
using System.Diagnostics;
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
		public static async Task Main()
		{
#if NETCOREAPP3_0
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var handler = new HttpEventProcessorHandler(new Uri("http://localhost:5000/"));
			var httpEventProcessor = handler.CreateEventProcessor();
			var options = new IoProcessorOptions
						  {
								  EventProcessors = new[] { httpEventProcessor },
								  ServiceFactories = new[] { WebBrowserService.GetFactory<CefSharpWebBrowserService>() },
								  DataModelHandlerFactories = new[] { EcmaScriptDataModelHandler.Factory },
						  };

			await using var ioProcessor = new IoProcessor(options);

#pragma warning disable 4014
			ioProcessor.Execute(sessionId: "captcha", GetCaptchaStateMachine());
#pragma warning restore 4014

			var webHost = new WebHostBuilder()
						  .Configure(builder => builder.Run(handler.ProcessRequest))
						  .UseKestrel()
						  .Build();

			await webHost.StartAsync().ConfigureAwait(false);

			Application.Run();
		}

		private static IStateMachine GetCaptchaStateMachine()
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.Core.Host.Scxml.Captcha.xml");
			Debug.Assert(stream != null, nameof(stream) + " != null");
			using var xmlReader = XmlReader.Create(stream);
			return new ScxmlDirector(xmlReader, new BuilderFactory()).ConstructStateMachine();
		}
	}
}