using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using TSSArt.StateMachine.EcmaScript;
using TSSArt.StateMachine.Services;

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

			var options = new IoProcessorOptions
						  {
								  EventProcessorFactories = ImmutableArray.Create((IEventProcessorFactory) new HttpEventProcessorFactory(baseUri, path: "/")),
								  ServiceFactories = ImmutableArray.Create(WebBrowserService.GetFactory<CefSharpWebBrowserService>(), InputService.Factory),
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory)
						  };

			await using var ioProcessor = new IoProcessor(options);

			foreach (var name in GetResourceNames())
			{
				var _ = ioProcessor.Execute(GetStateMachine(name));
			}

			await ioProcessor.StartAsync().ConfigureAwait(false);

			Application.Run();

			await ioProcessor.StopAsync().ConfigureAwait(false);
		}

		private static IEnumerable<string> GetResourceNames()
		{
			var prefix = Assembly.GetExecutingAssembly().GetName().Name + ".Autorun.";
			const string suffix = ".xml";
			foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
			{
				if (name.StartsWith(prefix) && name.EndsWith(suffix))
				{
					yield return name;
				}
			}
		}

		private static IStateMachine GetStateMachine(string name)
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
			using var xmlReader = XmlReader.Create(stream ?? throw new MissingManifestResourceException());
			return new ScxmlDirector(xmlReader, BuilderFactory.Default, DefaultErrorProcessor.Instance).ConstructStateMachine();
		}
	}
}