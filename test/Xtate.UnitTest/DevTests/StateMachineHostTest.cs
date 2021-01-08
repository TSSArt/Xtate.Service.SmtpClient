#region Copyright © 2019-2020 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.CustomAction;
using Xtate.DataModel.EcmaScript;
using Xtate.Service;

namespace Xtate.Test
{
	[TestClass]
	public class StateMachineHostTest
	{
		private static Stream GetStateMachineBase(string scxml) => new MemoryStream(Encoding.ASCII.GetBytes(scxml));

		private static Stream GetStateMachine(string xml) => GetStateMachineBase("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

		[TestMethod]
		public async Task SimpleTest()
		{
			var resourceLoaderMock = new Mock<IResourceLoader>();
			var resourceLoaderActivatorMock = new Mock<IResourceLoaderFactoryActivator>();
			var resourceLoaderFactoryMock = new Mock<IResourceLoaderFactory>();

			var task = new ValueTask<Resource>(new Resource(new MemoryStream(Encoding.ASCII.GetBytes("content")), new ContentType()));

			resourceLoaderActivatorMock
					.Setup(e => e.CreateResourceLoader(It.IsAny<IFactoryContext>(), It.IsAny<CancellationToken>()))
					.Returns(new ValueTask<IResourceLoader>(resourceLoaderMock.Object));

			resourceLoaderFactoryMock
					.Setup(e => e.TryGetActivator(It.IsAny<IFactoryContext>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
					.Returns(new ValueTask<IResourceLoaderFactoryActivator?>(resourceLoaderActivatorMock.Object));

			resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<NameValueCollection>(), It.IsAny<CancellationToken>())).Returns(task);

			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ResourceLoaderFactories = ImmutableArray.Create(resourceLoaderFactoryMock.Object)
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

			var resourceLoaderMock = new Mock<IResourceLoader>();
			var resourceLoaderActivatorMock = new Mock<IResourceLoaderFactoryActivator>();
			var resourceLoaderFactoryMock = new Mock<IResourceLoaderFactory>();

			var task = new ValueTask<Resource>(new Resource(stateMachine, new ContentType()));

			resourceLoaderActivatorMock
					.Setup(e => e.CreateResourceLoader(It.IsAny<IFactoryContext>(), It.IsAny<CancellationToken>()))
					.Returns(new ValueTask<IResourceLoader>(resourceLoaderMock.Object));

			resourceLoaderFactoryMock
					.Setup(e => e.TryGetActivator(It.IsAny<IFactoryContext>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
					.Returns(new ValueTask<IResourceLoaderFactoryActivator?>(resourceLoaderActivatorMock.Object));

			resourceLoaderMock.Setup(e => e.Request(new Uri("scxml://a"), It.IsAny<NameValueCollection>(), It.IsAny<CancellationToken>())).Returns(task);

			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ResourceLoaderFactories = ImmutableArray.Create(resourceLoaderFactoryMock.Object)
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

			var resourceLoaderMock = new Mock<IResourceLoader>();
			var resourceLoaderActivatorMock = new Mock<IResourceLoaderFactoryActivator>();
			var resourceLoaderFactoryMock = new Mock<IResourceLoaderFactory>();

			var task = new ValueTask<Resource>(new Resource(stateMachine, new ContentType()));

			resourceLoaderActivatorMock
					.Setup(e => e.CreateResourceLoader(It.IsAny<IFactoryContext>(), It.IsAny<CancellationToken>()))
					.Returns(new ValueTask<IResourceLoader>(resourceLoaderMock.Object));

			resourceLoaderFactoryMock
					.Setup(e => e.TryGetActivator(It.IsAny<IFactoryContext>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
					.Returns(new ValueTask<IResourceLoaderFactoryActivator?>(resourceLoaderActivatorMock.Object));

			resourceLoaderMock.Setup(e => e.Request(new Uri("scxml://a"), It.IsAny<NameValueCollection>(), It.IsAny<CancellationToken>())).Returns(task);


			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ServiceFactories = ImmutableArray.Create(HttpClientServiceFactory.Instance),
								  ResourceLoaderFactories = ImmutableArray.Create(resourceLoaderFactoryMock.Object)
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
								  ServiceFactories = ImmutableArray.Create(HttpClientServiceFactory.Instance,
																		   SmtpClientServiceFactory.Instance /*WebBrowserService.GetFactory<CefSharpWebBrowserService>()*/),
								  CustomActionFactories = ImmutableArray.Create(BasicCustomActionFactory.Instance, MimeCustomActionFactory.Instance)
						  };

			var stateMachineHost = new StateMachineHost(options);

			var result = await stateMachineHost.ExecuteStateMachineAsync(stateMachine);

			Console.WriteLine(DataModelConverter.ToJson(result));
			Assert.IsFalse(result.IsUndefined());
		}
	}
}