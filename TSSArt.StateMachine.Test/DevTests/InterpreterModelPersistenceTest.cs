using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class InterpreterModelPersistenceTest
	{
		private IStateMachine     _allStateMachine;
		private IDataModelHandler _dataModelHandler;

		[TestInitialize]
		public void Initialize()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSSArt.StateMachine.Test.Resources.All.xml");
			Debug.Assert(stream != null);

			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, BuilderFactory.Instance, DefaultErrorProcessor.Instance);

			_allStateMachine = director.ConstructStateMachine(StateMachineValidator.Instance);

			_dataModelHandler = EcmaScriptDataModelHandler.Factory.CreateHandler(DefaultErrorProcessor.Instance);
		}

		[TestMethod]
		public void SaveInterpreterModelTest()
		{
			var model = new InterpreterModelBuilder(_allStateMachine, _dataModelHandler, customActionProviders: default, DefaultErrorProcessor.Instance).Build();
			var storeSupport = model.Root.As<IStoreSupport>();

			var storage = new InMemoryStorage(false);
			storeSupport.Store(new Bucket(storage));

			new StateMachineReader().Build(new Bucket(storage));
		}

		[TestMethod]
		public void SaveRestoreInterpreterModelWithStorageRecreateTest()
		{
			var model = new InterpreterModelBuilder(_allStateMachine, _dataModelHandler, customActionProviders: default, DefaultErrorProcessor.Instance).Build();
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

			new InterpreterModelBuilder(restoredStateMachine, _dataModelHandler, customActionProviders: default, DefaultErrorProcessor.Instance).Build();
		}

		[TestMethod]
		public void SaveRestoreInterpreterModelRuntimeModelTest()
		{
			var _ = new StateMachineFluentBuilder(BuilderFactory.Instance)
					.BeginState((Identifier) "a")
					.AddTransition(context => true, (Identifier) "a")
					.AddOnEntry(context => Console.WriteLine(@"OnEntry"))
					.EndState()
					.Build();

			var model = new InterpreterModelBuilder(_allStateMachine, _dataModelHandler, customActionProviders: default, DefaultErrorProcessor.Instance).Build();
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

			new InterpreterModelBuilder(restoredStateMachine, _dataModelHandler, customActionProviders: default, DefaultErrorProcessor.Instance).Build();
		}
	}
}