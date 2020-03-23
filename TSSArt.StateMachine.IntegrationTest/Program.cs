using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TSSArt.StateMachine.EcmaScript;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine.IntegrationTest
{
	internal static class Program
	{
		private static async Task Main(string[] args)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());

			var baseUri = new Uri(args.Length > 0 ? args[0] : "http://localhost:5001/");

			var configurationBuilder = ImmutableDictionary.CreateBuilder<string, string>();
			configurationBuilder["uiEndpoint"] = "http://localhost:5000/dialog";
			configurationBuilder["mailEndpoint"] = "http://mid.dev.tssart.com/MailServer/Web2/api/Mail/";

			var options = new IoProcessorOptions
						  {
								  EventProcessorFactories = ImmutableArray.Create((IEventProcessorFactory) new HttpEventProcessorFactory(baseUri, path: "/")),
								  ServiceFactories = ImmutableArray.Create(HttpClientService.Factory, SmtpClientService.Factory),
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  CustomActionFactories = ImmutableArray.Create(BasicCustomActionFactory.Instance, MimeCustomActionFactory.Instance, MidCustomActionFactory.Instance),
								  ResourceLoader = new ResourceProvider(),
								  Configuration = configurationBuilder.ToImmutable()
						  };

			await using var ioProcessor = new IoProcessor(options);

			await ioProcessor.StartAsync().ConfigureAwait(false);

			dynamic prms = new DataModelObject();
			prms.loginUrl = "https://test.tssart.com/wp-login.php";
			prms.username = "tadex1";
			prms.password = "123456";

			var task = ioProcessor.Execute(new Uri(uriString: "signup", UriKind.Relative), prms);


			var result = await task.ConfigureAwait(false);

			dynamic prms2 = new DataModelObject();
			prms2.profileUrl = "https://test.tssart.com/wp-admin/profile.php";
			prms2.cookies = result.data.cookies;

			var task2 = ioProcessor.Execute(new Uri(uriString: "captureEmail", UriKind.Relative), new DataModelValue(prms2));

			dynamic _ = await task2.ConfigureAwait(false);

			await ioProcessor.StopAsync().ConfigureAwait(false);
		}
	}

	internal class ResourceProvider : IResourceLoader
	{
	#region Interface IResourceLoader

		public ValueTask<Resource> Request(Uri uri, CancellationToken token) => throw new NotSupportedException();

		public ValueTask<XmlReader> RequestXmlReader(Uri uri, XmlReaderSettings readerSettings = null, XmlParserContext parserContext = null, CancellationToken token = default)
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.IntegrationTest." + uri + ".xml");
			return new ValueTask<XmlReader>(XmlReader.Create(stream, readerSettings, parserContext));
		}

	#endregion
	}
}