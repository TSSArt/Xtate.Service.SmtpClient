#region Copyright © 2019-2021 Sergii Artemenko

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
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Builder;
using Xtate.Core;
using Xtate.IoC;
using Xtate.DataModel.EcmaScript;
using Xtate.Scxml;

namespace Xtate.Test
{
	[TestClass]
	public class ScxmlDirectorTest
	{
		private IStateMachine _stateMachine = default!;

		[TestInitialize]
		public void Initialize()
		{
			//TODO :delete
			/*var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Xtate.UnitTest.Resources.Main.xml");

			XmlNameTable nt = new NameTable();
			var xmlNamespaceManager = new XmlNamespaceManager(nt);
			var xmlReader = XmlReader.Create(stream!, settings: null, new XmlParserContext(nt, xmlNamespaceManager, xmlLang: default, xmlSpace: default));

			var serviceLocator = ServiceLocator.Create(
				delegate(IServiceCollection s)
				{
					s.AddForwarding<IStateMachineValidator, StateMachineValidator>();
					s.AddXPath();
				});
			var director = serviceLocator.GetService<ScxmlDirector, XmlReader>(xmlReader);
			//var director = new ScxmlDirector(xmlReader, serviceLocator.GetService<IBuilderFactory>(), new ScxmlDirectorOptions(serviceLocator) { NamespaceResolver = xmlNamespaceManager });

			_stateMachine = director.ConstructStateMachine().SynchronousGetResult();*/
		}

		private static EventObject CreateEventObject(string name) => new() { Type = EventType.External, NameParts = EventName.ToParts(name) };

		[TestMethod]
		public async Task ReadScxmlTest()
		{
			var services = new ServiceCollection();
			services.AddForwarding<IStateMachineLocation>(_ => new StateMachineLocation(new Uri(@"res://Xtate.UnitTest/Xtate.UnitTest/Resources/Main.xml")));
			services.RegisterStateMachineFactory();
			services.RegisterStateMachineInterpreter();
			services.RegisterEcmaScriptDataModelHandler();

			var serviceProvider = services.BuildProvider();

			var stateMachineInterpreter = await serviceProvider.GetRequiredService<IStateMachineInterpreter>();
			var eventQueueWriter = await serviceProvider.GetRequiredService<IEventQueueWriter>();


			await eventQueueWriter.WriteAsync(CreateEventObject("Event1"));
			await eventQueueWriter.WriteAsync(CreateEventObject("Test1.done"));
			await eventQueueWriter.WriteAsync(CreateEventObject("Event2"));
			await eventQueueWriter.WriteAsync(CreateEventObject("done.state.Test2"));
			await eventQueueWriter.WriteAsync(CreateEventObject("Timer"));
			//eventQueueWriter.Complete(new ArgumentException("333"));
			eventQueueWriter.Complete();

			var extComm = new Mock<IExternalCommunication>();
			var options = new InterpreterOptions(ServiceLocator.Create(
													 delegate(IServiceCollection s)
													 {
														 s.AddXPath();
														 s.AddEcmaScript();
													 }))
						  {
							  ExternalCommunication = extComm.Object
						  };

			try
			{
				await stateMachineInterpreter.RunAsync();

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
			var services = new ServiceCollection();
			services.AddForwarding<IStateMachineLocation>(_ => new StateMachineLocation(new Uri(@"res://Xtate.UnitTest/Xtate.UnitTest/Resources/Main.xml")));
			services.RegisterStateMachineFactory();
			services.RegisterStateMachineInterpreter();
			services.RegisterEcmaScriptDataModelHandler();

			var serviceProvider = services.BuildProvider();

			var stateMachineInterpreter = await serviceProvider.GetRequiredService<IStateMachineInterpreter>();
			var eventQueueWriter = await serviceProvider.GetRequiredService<IEventQueueWriter>();

			eventQueueWriter.Complete();
			//eventQueueWriter.Complete(new ArgumentException("333"));

			var options = new InterpreterOptions(ServiceLocator.Create(
													 delegate(IServiceCollection s)
													 {
														 s.AddXPath();
														 s.AddEcmaScript();
													 }))
						  {
							  
						  };

			try
			{
				await stateMachineInterpreter.RunAsync();

				Assert.Fail("StateMachineQueueClosedException should be raised");
			}
			catch (StateMachineQueueClosedException)
			{
				// ignore
			}
		}

		[TestMethod]
		public async Task ScxmlSerializerTest()
		{
			var services = new ServiceCollection();
			services.AddForwarding<IStateMachineLocation>(_ => new StateMachineLocation(new Uri(@"res://Xtate.UnitTest/Xtate.UnitTest/Resources/Main.xml")));
			services.RegisterStateMachineFactory();
			services.RegisterScxml();
			services.RegisterInterpreterModelBuilder();
			services.RegisterEcmaScriptDataModelHandler();

			var serviceProvider = services.BuildProvider();

			var modelBuilder = await serviceProvider.GetRequiredService<InterpreterModelBuilder>();
			var stateMachine = await serviceProvider.GetRequiredService<IStateMachine>();
			var scxmlSerializer = await serviceProvider.GetRequiredService<IScxmlSerializer>();


			var serviceLocator = ServiceLocator.Create(
				delegate(IServiceCollection s)
				{
					s.AddXPath();
					s.AddEcmaScript();
				});
			//var dataModelHandler = serviceLocator.GetService<EcmaScriptDataModelHandler>();
			//var parameters = new InterpreterModelBuilder.Parameters(serviceLocator, _stateMachine, dataModelHandler);
			//var interpreterModelBuilder = new InterpreterModelBuilder(parameters);

			var interpreterModel = await modelBuilder.Build3(stateMachine);
			var text = new StringWriter();
			var xmlWriter = XmlWriter.Create(text, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true });

			await scxmlSerializer.Serialize(interpreterModel.Root, xmlWriter);

			Console.WriteLine(text);
		}
	}
}