using System;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class InterpreterModelPersistenceTest
	{
		private IStateMachine           _allStateMachine;
		private IDataModelHandler       _dataModelHandler;
		private InterpreterModelBuilder _imBuilder;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.Test.Resources.All.xml");
			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, new BuilderFactory());

			_allStateMachine = director.ConstructStateMachine();

			_imBuilder = new InterpreterModelBuilder();
			_dataModelHandler = EcmaScriptDataModelHandler.Factory.CreateHandler(_imBuilder);
		}

		[TestMethod]
		public void SaveInterpreterModelTest()
		{
			var model = _imBuilder.Build(_allStateMachine, _dataModelHandler, customActionProviders: null);
			var storeSupport = model.Root.As<IStoreSupport>();

			var storage = new InMemoryStorage(false);
			storeSupport.Store(new Bucket(storage));

			new StateMachineReader().Build(new Bucket(storage));
		}

		[TestMethod]
		public void SaveRestoreInterpreterModelWithStorageRecreateTest()
		{
			var model = _imBuilder.Build(_allStateMachine, _dataModelHandler, customActionProviders: null);
			var storeSupport = model.Root.As<IStoreSupport>();

			byte[] transactionLog;
			using (var storage = new InMemoryStorage(false))
			{
				storeSupport.Store(new Bucket(storage));
				transactionLog = new byte[storage.GetTransactionLogSize()];
				storage.WriteTransactionLogToSpan(new Span<byte>(transactionLog));

				Assert.AreEqual(expected: 0, storage.GetTransactionLogSize());
			}

			IStateMachine restoredStateMachine;
			using (var newStorage = new InMemoryStorage(transactionLog))
			{
				restoredStateMachine = new StateMachineReader().Build(new Bucket(newStorage));
			}

			_imBuilder.Build(restoredStateMachine, _dataModelHandler, customActionProviders: null);
		}

		[TestMethod]
		public void SaveRestoreInterpreterModelRuntimeModelTest()
		{
			var stateMachine = new StateMachineFluentBuilder(new BuilderFactory())
							   .BeginState((Identifier) "a")
							   .AddTransition(context => true, (Identifier) "a")
							   .AddOnEntry(context => Console.WriteLine("OnEntry"))
							   .EndState()
							   .Build();

			var model = _imBuilder.Build(stateMachine, _dataModelHandler, customActionProviders: null);
			var storeSupport = model.Root.As<IStoreSupport>();

			byte[] transactionLog;
			using (var storage = new InMemoryStorage(false))
			{
				storeSupport.Store(new Bucket(storage));
				transactionLog = new byte[storage.GetTransactionLogSize()];
				storage.WriteTransactionLogToSpan(new Span<byte>(transactionLog));
			}

			IStateMachine restoredStateMachine;
			using (var newStorage = new InMemoryStorage(transactionLog))
			{
				restoredStateMachine = new StateMachineReader().Build(new Bucket(newStorage), model.EntityMap);
			}

			_imBuilder.Build(restoredStateMachine, _dataModelHandler, customActionProviders: null);
		}
	}
}