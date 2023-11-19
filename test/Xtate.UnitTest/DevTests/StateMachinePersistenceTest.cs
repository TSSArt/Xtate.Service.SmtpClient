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
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Core;
using Xtate.IoC;
using Xtate.Persistence;
using Xtate.Scxml;

namespace Xtate.Test
{
	[TestClass]
	public class StateMachinePersistenceTest
	{
		private IStateMachine                _allStateMachine           = default!;
		private Mock<IExternalCommunication> _externalCommunication     = default!;
		private Mock<IResourceLoaderFactory> _resourceLoaderFactoryMock = default!;
		private Mock<IResourceLoader> _resourceLoaderServiceMock = default!;
		private ServiceLocator _serviceLocator;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Xtate.UnitTest.Resources.All.xml");


			XmlNameTable nt = new NameTable();
			var xmlNamespaceManager = new XmlNamespaceManager(nt);
			using var xmlReader = XmlReader.Create(stream!, settings: null, new XmlParserContext(nt, xmlNamespaceManager, xmlLang: default, xmlSpace: default));

			var serviceLocator = ServiceLocator.Create(s => s.AddForwarding<IStateMachineValidator, StateMachineValidator>());
			//var director = serviceLocator.GetService<ScxmlDirector, XmlReader>(xmlReader);
			//var director = new ScxmlDirector(xmlReader, serviceLocator.GetService<IBuilderFactory>(), new ScxmlDirectorOptions(serviceLocator) { NamespaceResolver = xmlNamespaceManager });

			var sc = new ServiceCollection();
			sc.RegisterStateMachineFactory();
			sc.AddForwarding<IStateMachineLocation>(_ => new StateMachineLocation(new Uri("res://Xtate.UnitTest/Xtate.UnitTest/Resources/All.xml")));
			var sp = sc.BuildProvider();
			
			_allStateMachine = sp.GetRequiredService<IStateMachine>().Result;

			var task = new ValueTask<Resource>(new Resource(new MemoryStream(Encoding.ASCII.GetBytes("'content'")), new ContentType()));
			var loaderMock = new Mock<IResourceLoader>();
			loaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<NameValueCollection>()))
					  .Returns(task);
			var activatorMock = new Mock<IResourceLoaderFactoryActivator>();
			activatorMock.Setup(e => e.CreateResourceLoader(It.IsAny<ServiceLocator>()))
						 .Returns(new ValueTask<IResourceLoader>(loaderMock.Object));
			_resourceLoaderFactoryMock = new Mock<IResourceLoaderFactory>();
			_resourceLoaderFactoryMock.Setup(e => e.TryGetActivator(It.IsAny<Uri>()))
									  .Returns(new ValueTask<IResourceLoaderFactoryActivator?>(activatorMock.Object));
			_externalCommunication = new Mock<IExternalCommunication>();

			_resourceLoaderServiceMock = new Mock<IResourceLoader>();
			_resourceLoaderServiceMock.Setup(e => e.Request(It.IsAny<Uri>(), default)).Returns(task);
			_resourceLoaderServiceMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<NameValueCollection>())).Returns(task);

			_serviceLocator = ServiceLocator.Create(
				delegate(IServiceCollection s)
				{
					s.AddForwarding(_ => _resourceLoaderServiceMock.Object);
					s.AddXPath();
					s.AddEcmaScript();
				});
		}

		[TestMethod]
		public async Task SaveRestoreInterpreterTest()
		{
			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete(new ArgumentException("333"));

			var channel2 = Channel.CreateUnbounded<IEvent>();
			channel2.Writer.Complete(new ArgumentException("444"));

			var options = new InterpreterOptions(_serviceLocator)
						  {
							  ResourceLoaderFactories = ImmutableArray.Create(_resourceLoaderFactoryMock.Object),
							  PersistenceLevel = PersistenceLevel.ExecutableAction,
							  StorageProvider = new TestStorage(),
							  ExternalCommunication = _externalCommunication.Object
						  };

			var newSessionId = SessionId.New();
			try
			{
				await StateMachineInterpreter.RunAsync(newSessionId, _allStateMachine, channel.Reader, options);
			}
			catch (StateMachineQueueClosedException)
			{
				//expected
			}

			try
			{
				await StateMachineInterpreter.RunAsync(newSessionId, stateMachine: null, channel2.Reader, options);
			}
			catch (StateMachineQueueClosedException)
			{
				//expected
			}
		}

		private class TestStorage : IStorageProvider
		{
			private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryStream>> _storage = new();

		#region Interface IStorageProvider

			public async ValueTask<ITransactionalStorage> GetTransactionalStorage(string? partition, string key)
			{
				if (string.IsNullOrEmpty(key)) throw new ArgumentException(message: @"Value cannot be null or empty.", nameof(key));

				var partitionStorage = _storage.GetOrAdd(partition ?? "", _ => new ConcurrentDictionary<string, MemoryStream>());
				var memStream = partitionStorage.GetOrAdd(key, _ => new MemoryStream());

				var streamStorage = new StreamStorage(memStream, disposeStream: false)
									{
										InMemoryStorageFactory = b => new InMemoryStorage(b),
										InMemoryStorageBaselineFactory = memory => new InMemoryStorage(memory.Span)
									};
				await streamStorage.Initialization;
				return streamStorage;
			}

			public ValueTask RemoveTransactionalStorage(string? partition, string key)
			{
				if (string.IsNullOrEmpty(key)) throw new ArgumentException(message: @"Value cannot be null or empty.", nameof(key));

				var partitionStorage = _storage.GetOrAdd(partition ?? "", _ => new ConcurrentDictionary<string, MemoryStream>());
				partitionStorage.TryRemove(key, out _);

				return default;
			}

			public ValueTask RemoveAllTransactionalStorage(string? partition)
			{
				_storage.TryRemove(partition ?? "", out _);

				return default;
			}

		#endregion
		}
	}
}