using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.DataModel.EcmaScript;

namespace Xtate.Test
{
	[TestClass]
	public class ScxmlDirectorTest
	{
		private IStateMachine _stateMachine = default!;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Xtate.UnitTest.Resources.Main.xml");

			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, BuilderFactory.Instance, DefaultErrorProcessor.Instance);

			_stateMachine = director.ConstructStateMachine(StateMachineValidator.Instance);
		}

		private static EventObject CreateEventObject(string name) => new EventObject(EventType.External, EventName.ToParts(name));

		[TestMethod]
		public async Task ReadScxmlTest()
		{
			var channel = Channel.CreateUnbounded<IEvent>();

			await channel.Writer.WriteAsync(CreateEventObject("Event1"));
			await channel.Writer.WriteAsync(CreateEventObject("Test1.done"));
			await channel.Writer.WriteAsync(CreateEventObject("Event2"));
			await channel.Writer.WriteAsync(CreateEventObject("done.state.Test2"));
			await channel.Writer.WriteAsync(CreateEventObject("Timer"));
			channel.Writer.Complete(new ArgumentException("333"));

			var extComm = new Mock<IExternalCommunication>();
			var options = new InterpreterOptions { DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory), ExternalCommunication = extComm.Object };

			try
			{
				await StateMachineInterpreter.RunAsync(SessionId.New(), _stateMachine, channel.Reader, options);

				Assert.Fail("StateMachineQueueClosedException should be raised");
			}
			catch (StateMachineQueueClosedException)
			{
				// ignored
			}
		}

		[TestMethod]
		public async Task ReadAllScxmlTest()
		{
			var channel = Channel.CreateUnbounded<IEvent>();

			channel.Writer.Complete(new ArgumentException("333"));

			var options = new InterpreterOptions { DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory) };

			try
			{
				await StateMachineInterpreter.RunAsync(SessionId.New(), _stateMachine, channel.Reader, options);

				Assert.Fail("StateMachineQueueClosedException should be raised");
			}
			catch (StateMachineQueueClosedException)
			{
				// ignore
			}
		}

		[TestMethod]
		public void ScxmlSerializerTest()
		{
			var dataModelHandler = EcmaScriptDataModelHandler.Factory.CreateHandler(DefaultErrorProcessor.Instance);
			var interpreterModelBuilder = new InterpreterModelBuilder(_stateMachine, dataModelHandler, customActionProviders: default, DefaultErrorProcessor.Instance);
			var interpreterModel = interpreterModelBuilder.Build();
			var text = new StringWriter();
			using (var xmlWriter = XmlWriter.Create(text, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
			{
				ScxmlSerializer.Serialize(interpreterModel.Root, xmlWriter);
			}

			Console.WriteLine(text);
		}
	}
}