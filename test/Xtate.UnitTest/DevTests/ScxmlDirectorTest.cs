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
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Xtate.UnitTest.Resources.Main.xml");

			XmlNameTable nt = new NameTable();
			var xmlNamespaceManager = new XmlNamespaceManager(nt);
			var xmlReader = XmlReader.Create(stream!, settings: null, new XmlParserContext(nt, xmlNamespaceManager, xmlLang: default, xmlSpace: default));

			var director = new ScxmlDirector(xmlReader, BuilderFactory.Instance,
											 new ScxmlDirectorOptions { StateMachineValidator = StateMachineValidator.Instance, NamespaceResolver = xmlNamespaceManager });

			_stateMachine = director.ConstructStateMachine().SynchronousGetResult();
		}

		private static EventObject CreateEventObject(string name) => new() { Type = EventType.External, NameParts = EventName.ToParts(name) };

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
		public async Task ScxmlSerializerTest()
		{
			var dataModelHandler = new EcmaScriptDataModelHandler();
			var interpreterModelBuilder = new InterpreterModelBuilder(_stateMachine, dataModelHandler!, customActionProviders: default, default!, default!,
																	  DefaultErrorProcessor.Instance, baseUri: default);
			var interpreterModel = await interpreterModelBuilder.Build(default);
			var text = new StringWriter();
			var xmlWriter = XmlWriter.Create(text, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true });

			ScxmlSerializer.Serialize(interpreterModel.Root, xmlWriter!);

			Console.WriteLine(text);
		}
	}
}