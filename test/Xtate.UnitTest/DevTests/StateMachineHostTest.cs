using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.DataModel.EcmaScript;
using Xtate.Services;

namespace Xtate.Test
{
	[TestClass]
	[SuppressMessage(category: "ReSharper", checkId: "StringLiteralTypo")]
	public class StateMachineHostTest
	{
		private static XmlReader GetStateMachineBase(string scxml)
		{
			var textReader = new StringReader(scxml);
			return XmlReader.Create(textReader, new XmlReaderSettings { CloseInput = true });
		}

		private static XmlReader GetStateMachine(string xml) => GetStateMachineBase("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

		[TestMethod]
		public async Task SimpleTest()
		{
			var resourceLoaderMock = new Mock<IResourceLoader>();

			var task = new ValueTask<Resource>(new Resource(new Uri("http://none"), new ContentType(), content: "content"));
			resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(task);

			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ResourceLoaders = ImmutableArray.Create(resourceLoaderMock.Object)
						  };

			var stateMachineHost = new StateMachineHost(options);
			await stateMachineHost.StartHostAsync();
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Xtate.UnitTest.Resources.All.xml");
			var reader = new StreamReader(stream ?? throw new InvalidOperationException());
			var _ = stateMachineHost.ExecuteStateMachineAsync(await reader.ReadToEndAsync());
		}

		[TestMethod]
		public async Task InputOutputTest()
		{
			var stateMachine = GetStateMachine("<datamodel><data id='dmValue' expr='111'/></datamodel><final id='fin'><donedata><content expr='dmValue'/></donedata></final>");

			var stateMachineProviderMock = new Mock<IResourceLoader>();
			stateMachineProviderMock.Setup(x => x.RequestXmlReader(new Uri("scxml://a"), It.IsAny<XmlReaderSettings>(), It.IsAny<XmlParserContext>(), It.IsAny<CancellationToken>()))
									.Returns(new ValueTask<XmlReader>(stateMachine));

			stateMachineProviderMock.Setup(x => x.CanHandle(It.IsAny<Uri>())).Returns(true);

			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ResourceLoaders = ImmutableArray.Create(stateMachineProviderMock.Object)
						  };

			var stateMachineHost = new StateMachineHost(options);
			await stateMachineHost.StartHostAsync();
			var result = await stateMachineHost.ExecuteStateMachineAsync(new Uri("scxml://a"));

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

			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ServiceFactories = ImmutableArray.Create(HttpClientService.Factory),
								  ResourceLoaders = ImmutableArray.Create(stateMachineProviderMock.Object)
						  };

			var stateMachineHost = new StateMachineHost(options);

			var result = await stateMachineHost.ExecuteStateMachineAsync(new Uri("scxml://a"));

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
		<finalize xmlns:basic='http://xtate.net/scxml/customaction/basic' xmlns:mime='http://xtate.net/scxml/customaction/mime'>
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

			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ServiceFactories = ImmutableArray.Create(HttpClientService.Factory, SmtpClientService.Factory /*WebBrowserService.GetFactory<CefSharpWebBrowserService>()*/),
								  CustomActionFactories = ImmutableArray.Create(BasicCustomActionFactory.Instance, MimeCustomActionFactory.Instance)
						  };

			var stateMachineHost = new StateMachineHost(options);

			var result = await stateMachineHost.ExecuteStateMachineAsync(stateMachine);

			Console.WriteLine(DataModelConverter.ToJson(result));
			Assert.IsFalse(result.IsUndefined());
		}
	}
}