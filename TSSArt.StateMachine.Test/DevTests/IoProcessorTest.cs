using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
{
	[SimpleService("https://www.w3.org/Protocols/HTTP/", Alias = "http")]
	public class HttpClientService : SimpleServiceBase
	{
		protected override async ValueTask<DataModelValue> Execute()
		{
			dynamic data = Parameters.ToObject();

			using var client = new HttpClient();
			var request = new HttpRequestMessage(new HttpMethod(data.method), Source);
			var headers = data.headers;
			if (headers != null)
			{
				foreach (var header in headers)
				{
					request.Headers.Add(header.name, header.value);
				}
			}

			var requestContent = data.content;
			if (requestContent != null)
			{
				request.Content = new StringContent(requestContent);
			}

			var result = await client.SendAsync(request, StopToken);
			var content = await result.Content.ReadAsStringAsync();

			return new DataModelValue(content);
		}
	}

	[TestClass]
	public class IoProcessorTest
	{
		private IStateMachine GetStateMachineBase(string scxml)
		{
			using var textReader = new StringReader(scxml);
			using var reader = XmlReader.Create(textReader);
			return new ScxmlDirector(reader, new BuilderFactory()).ConstructStateMachine();
		}

		private IStateMachine GetStateMachine(string xml) => GetStateMachineBase("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

		[TestMethod]
		public void SimpleTest()
		{
			var resourceLoaderMock = new Mock<IResourceLoader>();

			var task = new ValueTask<Resource>(new Resource(new Uri("http://none"), new ContentType(), content: "content"));
			resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(task);

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = new List<IDataModelHandlerFactory>(),
								  StateMachineProvider = new StateMachineProvider(),
								  ResourceLoader = resourceLoaderMock.Object
						  };
			options.DataModelHandlerFactories.Add(EcmaScriptDataModelHandler.Factory);

			var ioProcessor = new IoProcessor(options);
			var _ = ioProcessor.Execute(new Uri(@"D:\Ser\Projects\T.S.S.Art\MID\PoC\TSSArt.StateMachine.Test\Resources\All.xml"));
		}

		[TestMethod]
		public async Task InputOutputTest()
		{
			var stateMachine = GetStateMachine("<datamodel><data id='dmValue' expr='111'/></datamodel><final id='fin'><donedata><content expr='dmValue'/></donedata></final>");

			var stateMachineProviderMock = new Mock<IStateMachineProvider>();
			stateMachineProviderMock.Setup(x => x.GetStateMachine(new Uri("scxml://a"))).Returns(new ValueTask<IStateMachine>(stateMachine));

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = new List<IDataModelHandlerFactory>(),
								  StateMachineProvider = stateMachineProviderMock.Object
						  };
			options.DataModelHandlerFactories.Add(EcmaScriptDataModelHandler.Factory);

			var ioProcessor = new IoProcessor(options);
			var result = await ioProcessor.Execute(new Uri("scxml://a"));

			Assert.AreEqual(expected: 111.0, result.AsNumber());
		}

		[TestMethod]
		public async Task HttpInvokeHttpGoogleComTest()
		{
			var stateMachine = GetStateMachine(@"
<state id='Intro'>
    <invoke id='tid' src='http://google.com' type='http'>
    <param name='method' expr=""'get'""/>
    <param name='headers' expr=""[{name: 'Accept', value: 'text/plain'}]""/>
    </invoke>
    <transition event='done.invoke.tid' target='fin'/>
</state>
<final id='fin'>
	<donedata><content expr='_event.data'/></donedata>
</final>
<final id='finErr'></final>");

			var stateMachineProviderMock = new Mock<IStateMachineProvider>();
			stateMachineProviderMock.Setup(x => x.GetStateMachine(new Uri("scxml://a"))).Returns(new ValueTask<IStateMachine>(stateMachine));

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = new List<IDataModelHandlerFactory>(),
								  ServiceFactories = new List<IServiceFactory>(),
								  StateMachineProvider = stateMachineProviderMock.Object
						  };

			options.ServiceFactories.Add(SimpleServiceFactory<HttpClientService>.Instance);
			options.DataModelHandlerFactories.Add(EcmaScriptDataModelHandler.Factory);

			var ioProcessor = new IoProcessor(options);

			dynamic args = new DataModelObject();
			args.method = "get";
			args.header = new DataModelObject();
			args.header.name = "value";
			args.content = "HTML";

			var fromObject = DataModelValue.FromObject((DataModelObject) args);
			var result = await ioProcessor.Execute(new Uri("scxml://a"), fromObject);

			//Console.WriteLine(result.ToString("J"));
			Assert.IsTrue(result.Type != DataModelValueType.Undefined);
		}
	}

	public class StateMachineProvider : IStateMachineProvider
	{
		public ValueTask<IStateMachine> GetStateMachine(Uri source)
		{
			using var stream = new FileStream(source.AbsolutePath, FileMode.Open);
			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, new BuilderFactory());

			return new ValueTask<IStateMachine>(director.ConstructStateMachine());
		}

		public ValueTask<IStateMachine> GetStateMachine(string scxml)
		{
			var reader = new StringReader(scxml);
			var xmlReader = XmlReader.Create(reader);

			var director = new ScxmlDirector(xmlReader, new BuilderFactory());

			return new ValueTask<IStateMachine>(director.ConstructStateMachine());
		}
	}
}