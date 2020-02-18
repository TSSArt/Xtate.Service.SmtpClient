using System;
using System.Collections.Immutable;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TSSArt.StateMachine.EcmaScript;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class IoProcessorTest
	{
		private XmlReader GetStateMachineBase(string scxml)
		{
			var textReader = new StringReader(scxml);
			return XmlReader.Create(textReader, new XmlReaderSettings { CloseInput = true });
		}

		private XmlReader GetStateMachine(string xml) => GetStateMachineBase("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

		[TestMethod]
		public void SimpleTest()
		{
			var resourceLoaderMock = new Mock<IResourceLoader>();

			var task = new ValueTask<Resource>(new Resource(new Uri("http://none"), new ContentType(), content: "content"));
			resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(task);

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ResourceLoader = resourceLoaderMock.Object
						  };

			var ioProcessor = new IoProcessor(options);
			var _ = ioProcessor.Execute(new Uri(@"D:\Ser\Projects\T.S.S.Art\MID\PoC\TSSArt.StateMachine.Test\Resources\All.xml"));
		}

		[TestMethod]
		public async Task InputOutputTest()
		{
			var stateMachine = GetStateMachine("<datamodel><data id='dmValue' expr='111'/></datamodel><final id='fin'><donedata><content expr='dmValue'/></donedata></final>");

			var stateMachineProviderMock = new Mock<IResourceLoader>();
			stateMachineProviderMock.Setup(x => x.RequestXmlReader(new Uri("scxml://a"), It.IsAny<XmlReaderSettings>(), It.IsAny<XmlParserContext>(), It.IsAny<CancellationToken>()))
									.Returns(new ValueTask<XmlReader>(stateMachine));

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ResourceLoader = stateMachineProviderMock.Object
						  };

			var ioProcessor = new IoProcessor(options);
			var result = await ioProcessor.Execute(new Uri("scxml://a"));

			Assert.AreEqual(expected: 111.0, result.AsNumber());
		}

		[TestMethod]
		[Ignore("Makes HTTP request. Not a unit test.")]
		public async Task HttpInvokeHttpGoogleComTest()
		{
			var stateMachine = GetStateMachine(@"
<state id='Intro'>
    <invoke id='tid' src='http://google.com' type='http'>
		<param name='autoRedirect' expr='true'/>
		<param name='method' expr=""'get'""/>
		<param name='headers' expr=""[{name: 'Accept', value: 'text/plain'}]""/>
		<param name='capture' expr=""

({
capture1: {xpath:'//div[@aria-owner]', attr:'id'}
})

""/>
    </invoke>
    <transition event='done.invoke.tid' target='fin'/>
    <transition event='error.invoke.tid' target='finErr'><log label='fail' /></transition>
</state>
<final id='fin'>
	<donedata><content expr='_event.data'/></donedata>
</final>
<final id='finErr'>
	<donedata><content expr='_event.data'/></donedata>
</final>");

			var stateMachineProviderMock = new Mock<IResourceLoader>();
			stateMachineProviderMock.Setup(x => x.RequestXmlReader(new Uri("scxml://a"), It.IsAny<XmlReaderSettings>(), It.IsAny<XmlParserContext>(), It.IsAny<CancellationToken>()))
									.Returns(new ValueTask<XmlReader>(stateMachine));

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ServiceFactories = ImmutableArray.Create(HttpClientService.Factory),
								  ResourceLoader = stateMachineProviderMock.Object
						  };

			var ioProcessor = new IoProcessor(options);

			var result = await ioProcessor.Execute(new Uri("scxml://a"));

			Console.WriteLine(DataModelConverter.ToJson(result));
			Assert.IsFalse(result.IsUndefined());
		}

		[TestMethod]
		[Ignore("Makes HTTP request. Not a unit test.")]
		public async Task HttpInvokeHttpSendEmailTest()
		{
			var stateMachine = StateMachineGenerator.FromInnerScxml_EcmaScript(@"
<datamodel>
	<data id=""email"" />
</datamodel>
<state id='RegisterEmail'>
    <invoke src='http://mid.dev.tssart.com/MailServer/Web2/api/Mail' type='http'>
		<param name='method' expr=""'post'""/>
		<param name='accept' expr=""'application/json'""/>
		<finalize>
			<assign location='email' expr='_event.data.content.Email' />
		</finalize>
    </invoke>
    <transition event='done.invoke' target='SendEmail'/>
    <transition event='error' target='finErr' />
</state>
<state id='SendEmail'>
    <invoke type='smtp'>
		<param name='server' expr=""'hare.tssart.com'""/>
		<param name='from' expr=""'ser@tssart.com'""/>
		<param name='to' expr='email'/>
		<param name='subject' expr=""'This is Subject'""/>
		<param name='body' expr=""'This is BODY'""/>
  	</invoke>
    <transition event='done.invoke' target='ReCaptcha'/>
    <transition event='error' target='finErr'/>
</state>
<state id='ReCaptcha'>
    <invoke src='https://www.tssart.com/wp-login.php' type='browser'>
		<param name='type' expr=""'GoogleRecaptchaV2'""/>
		<param name='sitekey' expr=""'6LdI3RYTAAAAAMkmqOQSdno04oej6pDHGFpU3TRG'""/>
    </invoke>
    <transition event='done.invoke' target='WaitForEmail'/>
    <transition event='error' target='finErr'/>
</state>
<state id='WaitForEmail'>
    <invoke srcexpr='&quot;http://mid.dev.tssart.com/MailServer/Web2/api/Mail?lastReceivedOnUtc=2019-01-01&amp;email=&quot; + email' type='http'>
		<param name='accept' expr=""'application/json'""/>
		<finalize xmlns:basic='http://tssart.com/scxml/customaction/basic' xmlns:mime='http://tssart.com/scxml/customaction/mime'>
			<assign location='emailContent' expr='_event.data.content.EmailEntries[0].ContentRaw' />
			<basic:base64decode source='emailContent' destination='emailContent' />
			<mime:parseEmail source='emailContent' destination='confirmationUrl' xpath='' attr='' regex='' />
		</finalize>
    </invoke>
    <transition event='done.invoke' target='fin'/>
    <transition event='error' target='finErr' />
</state>
<final id='fin'>
	<donedata><content expr='_event.data'/></donedata>
</final>
<final id='finErr'>
	<donedata><content expr='_event.data'/></donedata>
</final>");

			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ServiceFactories = ImmutableArray.Create(HttpClientService.Factory, SmtpClientService.Factory /*WebBrowserService.GetFactory<CefSharpWebBrowserService>()*/),
								  CustomActionProviders = ImmutableArray.Create(BasicCustomActionProvider.Instance, MimeCustomActionProvider.Instance)
						  };

			var ioProcessor = new IoProcessor(options);

			var result = await ioProcessor.Execute(stateMachine);

			Console.WriteLine(DataModelConverter.ToJson(result));
			Assert.IsFalse(result.IsUndefined());
		}
	}

	public class StateMachineProvider : IResourceLoader
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

		public ValueTask<Resource> Request(Uri uri, CancellationToken token) => throw new NotSupportedException();

		public ValueTask<XmlReader> RequestXmlReader(Uri uri, XmlReaderSettings readerSettings = null, XmlParserContext parserContext = null, CancellationToken token = default)
		{
			return new ValueTask<XmlReader>(XmlReader.Create(uri.ToString(), readerSettings, parserContext));
		}
	}
}