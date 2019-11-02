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
	public class HttpClientServiceFactory : IServiceFactory
	{
		private static readonly Uri ServiceFactoryTypeId      = new Uri("https://www.w3.org/Protocols/HTTP/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "http", UriKind.Relative);

		public Uri TypeId => ServiceFactoryTypeId;

		public Uri AliasTypeId => ServiceFactoryAliasTypeId;

		public ValueTask<IService> StartService(Uri source, DataModelValue data, CancellationToken token) => new ValueTask<IService>(new HttpClientService(source, data));
	}

	public class HttpClientService : IService
	{
		private readonly TaskCompletionSource<DataModelValue> _completedCompletionSource = new TaskCompletionSource<DataModelValue>();
		private readonly CancellationTokenSource              _tokenSource               = new CancellationTokenSource();

		public HttpClientService(Uri source, DataModelValue data)
		{
			RunAsync(source, data.ToObject());
		}

		public ValueTask Send(IEvent @event, CancellationToken token) => throw new NotSupportedException("Events not supported");

		public ValueTask Destroy(CancellationToken token)
		{
			_tokenSource.Cancel();
			_completedCompletionSource.TrySetCanceled();

			return default;
		}

		public ValueTask<DataModelValue> Result => new ValueTask<DataModelValue>(_completedCompletionSource.Task);

		private async void RunAsync(Uri source, dynamic data)
		{
			try
			{
				using var client = new HttpClient();
				var request = new HttpRequestMessage(new HttpMethod(data.method), source);
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

				var result = await client.SendAsync(request, _tokenSource.Token);
				var content = await result.Content.ReadAsStringAsync();

				_completedCompletionSource.TrySetResult(new DataModelValue(content));
			}
			catch (OperationCanceledException ex)
			{
				_completedCompletionSource.TrySetCanceled(ex.CancellationToken);
			}
			catch (Exception ex)
			{
				_completedCompletionSource.TrySetException(ex);
			}
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
			stateMachineProviderMock.Setup(x => x.GetStateMachine(new Uri("scxml://a"))).Returns(stateMachine);

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
    <transition event='error' target='finErr'><log label='Error Event' expr='_event'/></transition>
</state>
<final id='fin'>
	<donedata><content expr='_event.data'/></donedata>
</final>
<final id='finErr'></final>");

			var stateMachineProviderMock = new Mock<IStateMachineProvider>();
			stateMachineProviderMock.Setup(x => x.GetStateMachine(new Uri("scxml://a"))).Returns(stateMachine);

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = new List<IDataModelHandlerFactory>(),
								  ServiceFactories = new List<IServiceFactory>(),
								  StateMachineProvider = stateMachineProviderMock.Object
						  };
			options.ServiceFactories.Add(new HttpClientServiceFactory());
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
		public IStateMachine GetStateMachine(Uri source)
		{
			using var stream = new FileStream(source.AbsolutePath, FileMode.Open);
			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, new BuilderFactory());

			return director.ConstructStateMachine();
		}
	}
}