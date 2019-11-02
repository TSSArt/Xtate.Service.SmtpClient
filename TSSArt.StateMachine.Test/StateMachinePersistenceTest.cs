using System;
using System.Collections.Generic;
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
	public class StateMachinePersistenceTest
	{
		private IStateMachine         _allStateMachine;
		private Mock<IResourceLoader> _resourceLoaderMock;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.Test.Resources.All.xml");
			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, new BuilderFactory());

			_allStateMachine = director.ConstructStateMachine();

			_resourceLoaderMock = new Mock<IResourceLoader>();
			var task = new ValueTask<Resource>(new Resource(new Uri("http://none"), new ContentType(), content: "content"));
			_resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(task);
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
								  DataModelHandlerFactories = new List<IDataModelHandlerFactory>(),
								  ResourceLoader = _resourceLoaderMock.Object,
								  PersistenceLevel = PersistenceLevel.ExecutableAction,
								  StorageProvider = new TestStorage()
						  };
			options.DataModelHandlerFactories.Add(EcmaScriptDataModelHandler.Factory);

			var newSessionId = IdGenerator.NewSessionId();
			var stateMachineResult = await StateMachineInterpreter.RunAsync(newSessionId, _allStateMachine, channel.Reader, options);

			var stateMachineResult2 = await StateMachineInterpreter.RunAsync(newSessionId, stateMachine: null, channel2.Reader, options);

			Assert.AreEqual(StateMachineExitStatus.QueueClosed, stateMachineResult.Status);
			Assert.AreEqual(StateMachineExitStatus.QueueClosed, stateMachineResult2.Status);
		}

		private class TestStorage : IStorageProvider
		{
			private readonly Dictionary<string, MemoryStream> _streams = new Dictionary<string, MemoryStream>();

			public async ValueTask<ITransactionalStorage> GetTransactionalStorage(string name, CancellationToken token)
			{
				if (!_streams.TryGetValue(name, out var stream))
				{
					stream = new MemoryStream();
					_streams.Add(name, stream);
				}

				return await StreamStorage.CreateAsync(stream, disposeStream: false, token);
			}

			public ValueTask RemoveTransactionalStorage(string name, CancellationToken token) => default;

			public ValueTask RemoveAllTransactionalStorage(CancellationToken token) => default;
		}
	}
}