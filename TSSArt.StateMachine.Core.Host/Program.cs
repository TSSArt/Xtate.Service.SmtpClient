using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
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

			var options = new IoProcessorOptionsBuilder()
						   .AddEcmaScript()
						   .AddCefSharpWebBrowser()
						   .AddUserInteraction()
						   .AddHttpEventProcessor(baseUri, path: "/")
						   .Build();

			await using var ioProcessor = new IoProcessor(options);

			await ioProcessor.StartAsync().ConfigureAwait(false);

			var machines = GetResourceNames().Select(name => ioProcessor.Execute(GetStateMachine(name))).ToList();

			Application.Run();

			await ioProcessor.StopAsync().ConfigureAwait(false);

			await Task.WhenAll(machines).ConfigureAwait(false);
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
			var errorProcessor = new DetailedErrorProcessor(sessionId: null, stateMachine: null, new Uri(name, UriKind.RelativeOrAbsolute), scxml: null);
			var stateMachine = new ScxmlDirector(xmlReader, BuilderFactory.Instance, errorProcessor).ConstructStateMachine(StateMachineValidator.Instance);
			errorProcessor.ThrowIfErrors();
			return stateMachine;
		}
	}
}