using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class ScxmlDirectorTest
	{
		private IStateMachine _stateMachine = default!;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.Test.Resources.Main.xml");

			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, BuilderFactory.Instance, DefaultErrorProcessor.Instance);

			_stateMachine = director.ConstructStateMachine(StateMachineValidator.Instance);
		}

		[TestMethod]
		public async Task ReadScxmlTest()
		{
			var channel = Channel.CreateUnbounded<IEvent>();

			await channel.Writer.WriteAsync(new EventObject("Event1"));
			await channel.Writer.WriteAsync(new EventObject("Test1.done"));
			await channel.Writer.WriteAsync(new EventObject("Event2"));
			await channel.Writer.WriteAsync(new EventObject("done.state.Test2"));
			await channel.Writer.WriteAsync(new EventObject("Timer"));
			channel.Writer.Complete(new ArgumentException("333"));

			var options = new InterpreterOptions { DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory) };

			try
			{
				await StateMachineInterpreter.RunAsync(IdGenerator.NewSessionId(), _stateMachine, channel.Reader, options);

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
				await StateMachineInterpreter.RunAsync(IdGenerator.NewSessionId(), _stateMachine, channel.Reader, options);

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