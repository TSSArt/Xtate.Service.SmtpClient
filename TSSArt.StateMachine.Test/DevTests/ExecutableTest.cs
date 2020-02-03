using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class ExecutableTest
	{
		private Mock<ICustomActionProvider>  _customActionProvider;
		private ChannelReader<IEvent>        _eventChannel;
		private Mock<IExternalCommunication> _externalCommunication;
		private Mock<ILogger>                _logger;
		private InterpreterOptions           _options;

		private IStateMachine GetStateMachine(string scxml)
		{
			using (var textReader = new StringReader(scxml))
			using (var reader = XmlReader.Create(textReader))
			{
				return new ScxmlDirector(reader, new BuilderFactory()).ConstructStateMachine();
			}
		}

		private IStateMachine NoneDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='none'>" + xml + "</scxml>");
		private IStateMachine EcmaDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + xml + "</scxml>");

		private async Task RunStateMachine(Func<string, IStateMachine> getter, string innerXml)
		{
			var stateMachine = getter(innerXml);

			await StateMachineInterpreter.RunAsync(IdGenerator.NewSessionId(), stateMachine, _eventChannel, _options);
		}

		[TestInitialize]
		public void Init()
		{
			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			_eventChannel = channel.Reader;

			_customActionProvider = new Mock<ICustomActionProvider>();
			_customActionProvider.Setup(x => x.GetAction(It.IsAny<string>()))
								 .Returns((context, token) =>
										  {
											  context.Log(label: "Custom");
											  return default;
										  });
			_customActionProvider.Setup(x => x.CanHandle(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

			_options = new InterpreterOptions
					   {
							   DataModelHandlerFactories = new List<IDataModelHandlerFactory> { EcmaScriptDataModelHandler.Factory },
							   CustomActionProviders = new List<ICustomActionProvider> { _customActionProvider.Object }
					   };
			_logger = new Mock<ILogger>();

			_options.Logger = _logger.Object;
			_externalCommunication = new Mock<IExternalCommunication>();
			_options.ExternalCommunication = _externalCommunication.Object;
		}

		[TestMethod]
		public async Task RaiseTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><raise event='my'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.Log(It.IsAny<string>(), It.IsAny<string>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task SendInternalTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><send event='my' target='_internal'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.Log(It.IsAny<string>(), It.IsAny<string>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task RaiseWithEventDescriptorTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.Log(It.IsAny<string>(), It.IsAny<string>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task RaiseWithEventDescriptor2Test()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><raise event='my.suffix'/></onentry><transition event='my.*' target='s2'/></state><state id='s2'><onentry><log label='Hello'/></onentry></state>");

			_logger.Verify(l => l.Log(It.IsAny<string>(), It.IsAny<string>(), "Hello", default, default), Times.Once);
		}

		[TestMethod]
		public async Task CustomActionTest()
		{
			await RunStateMachine(NoneDataModel,
								  innerXml:
								  "<state id='s1'><onentry><custom my='name'/></onentry></state>");

			_logger.Verify(l => l.Log(It.IsAny<string>(), It.IsAny<string>(), "Custom", default, default), Times.Once);
		}

		[TestMethod]
		public async Task ContentJsonTest()
		{
			await RunStateMachine(EcmaDataModel,
								  innerXml:
								  "<state id='s1'><onentry><send><content>{ 'key':'value' }</content></send></onentry></state>");

			_externalCommunication.Verify(a => a.TrySendEvent(It.IsAny<IOutgoingEvent>(), It.IsAny<CancellationToken>()));
		}
	}
}