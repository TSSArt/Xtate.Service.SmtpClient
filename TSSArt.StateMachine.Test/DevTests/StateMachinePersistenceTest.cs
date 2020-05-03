using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Reflection;
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
	[SuppressMessage(category: "ReSharper", checkId: "RedundantCapturedContext")]
	public class StateMachinePersistenceTest
	{
		private IStateMachine         _allStateMachine    = default!;
		private Mock<IResourceLoader> _resourceLoaderMock = default!;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.Test.Resources.All.xml");

			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, BuilderFactory.Instance, DefaultErrorProcessor.Instance);

			_allStateMachine = director.ConstructStateMachine(StateMachineValidator.Instance);

			_resourceLoaderMock = new Mock<IResourceLoader>();
			var task = new ValueTask<Resource>(new Resource(new Uri("http://none"), new ContentType(), content: "content"));
			_resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(task);
			_resourceLoaderMock.Setup(e => e.CanHandle(It.IsAny<Uri>())).Returns(true);
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
								  StorageProvider = new TestStorage()
						  };

			var newSessionId = IdGenerator.NewSessionId();
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