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
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Builder;
using Xtate.DataModel.EcmaScript;
using Xtate.Persistence;
using Xtate.Scxml;

namespace Xtate.Test
{
	[TestClass]
	public class StateMachinePersistenceTest
	{
		private IStateMachine                _allStateMachine       = default!;
		private Mock<IExternalCommunication> _externalCommunication = default!;
		private Mock<IResourceLoader>        _resourceLoaderMock    = default!;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Xtate.UnitTest.Resources.All.xml");


			XmlNameTable nt = new NameTable();
			var xmlNamespaceManager = new XmlNamespaceManager(nt);
			using var xmlReader = XmlReader.Create(stream!, settings: null, new XmlParserContext(nt, xmlNamespaceManager, xmlLang: default, xmlSpace: default));

			var director = new ScxmlDirector(xmlReader, BuilderFactory.Instance, DefaultErrorProcessor.Instance, xmlNamespaceManager);


			_allStateMachine = director.ConstructStateMachine(StateMachineValidator.Instance);

			_resourceLoaderMock = new Mock<IResourceLoader>();
			var task = new ValueTask<Resource>(new Resource(new Uri("http://none"), new ContentType(), content: "'content'"));
			_resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(task);
			_resourceLoaderMock.Setup(e => e.CanHandle(It.IsAny<Uri>())).Returns(true);
			_externalCommunication = new Mock<IExternalCommunication>();
		}

		[TestMethod]
		public async Task SaveRestoreInterpreterTest()
		{
			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete(new ArgumentException("333"));

			var channel2 = Channel.CreateUnbounded<IEvent>();
			channel2.Writer.Complete(new ArgumentException("444"));

			var options = new InterpreterOptions
						  {
								  DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory),
								  ResourceLoaders = ImmutableArray.Create(_resourceLoaderMock.Object),
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
			private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryStream>> _storage = new ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryStream>>();

		#region Interface IStorageProvider

			public async ValueTask<ITransactionalStorage> GetTransactionalStorage(string? partition, string key, CancellationToken token)
			{
				if (string.IsNullOrEmpty(key)) throw new ArgumentException(message: @"Value cannot be null or empty.", nameof(key));

				var partitionStorage = _storage.GetOrAdd(partition ?? "", p => new ConcurrentDictionary<string, MemoryStream>());
				var memStream = partitionStorage.GetOrAdd(key, k => new MemoryStream());

				return await StreamStorage.CreateAsync(memStream, disposeStream: false, token);
			}

			public ValueTask RemoveTransactionalStorage(string? partition, string key, CancellationToken token)
			{
				if (string.IsNullOrEmpty(key)) throw new ArgumentException(message: @"Value cannot be null or empty.", nameof(key));

				var partitionStorage = _storage.GetOrAdd(partition ?? "", p => new ConcurrentDictionary<string, MemoryStream>());
				partitionStorage.TryRemove(key, out _);

				return default;
			}

			public ValueTask RemoveAllTransactionalStorage(string? partition, CancellationToken token)
			{
				_storage.TryRemove(partition ?? "", out _);

				return default;
			}

		#endregion
		}
	}
}