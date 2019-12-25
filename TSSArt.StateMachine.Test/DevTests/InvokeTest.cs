using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class InvokeTest
	{
		private Mock<IExternalCommunication> _externalCommunicationMock;
		private Mock<ILogger>                _loggerMock;
		private InterpreterOptions           _options;
		private StateMachine                 _stateMachine;

		[TestInitialize]
		public void Initialize()
		{
			_stateMachine = new StateMachine
							{
									States = new IStateEntity[]
											 {
													 new State
													 {
															 Id = (Identifier) "S1",
															 Invoke = new IInvoke[]
																	  {
																			  new Invoke
																			  {
																					  Id = "invoke_id",
																					  Source = new Uri("proto://src"),
																					  Type = new Uri("proto://type"),
																					  Content = new Content { Body = new ContentBody { Value = "content" } },
																					  Finalize = new Finalize
																								 {
																										 Action = new IExecutableEntity[]
																												  {
																														  new Log
																														  {
																																  Label = "FinalizeExecuted"
																														  }
																												  }
																								 }
																			  }
																	  },
															 Transitions = new ITransition[]
																		   {
																				   new Transition
																				   {
																						   Event = new EventDescriptor[] { "ToF" },
																						   Target = new IIdentifier[] { (Identifier) "F" }
																				   }
																		   }
													 },
													 new Final
													 {
															 Id = (Identifier) "F"
													 }
											 }
							};

			_externalCommunicationMock = new Mock<IExternalCommunication>();
			_externalCommunicationMock.Setup(e => e.GetIoProcessors()).Returns(Array.Empty<IEventProcessor>());
			_externalCommunicationMock.Setup(e => e.IsInvokeActive(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
			_loggerMock = new Mock<ILogger>();

			_options = new InterpreterOptions
					   {
							   ExternalCommunication = _externalCommunicationMock.Object,
							   Logger = _loggerMock.Object
					   };
		}

		[TestMethod]
		public async Task SimpleTest()
		{
			var invokeUniqueId = "";
			_externalCommunicationMock.Setup(l => l.StartInvoke(It.IsAny<InvokeData>(), default))
									  .Callback((InvokeData data, CancellationToken token) =>  invokeUniqueId = data.InvokeUniqueId);

			var channel = Channel.CreateUnbounded<IEvent>();
			var task = StateMachineInterpreter.RunAsync(sessionId: "session1", _stateMachine, channel, _options);
			await channel.Writer.WriteAsync(new EventObject(name: "fromInvoked", invokeId: "invoke_id", invokeUniqueId));
			await channel.Writer.WriteAsync(new EventObject("ToF"));
			await task;


			_externalCommunicationMock.Verify(l => l.GetIoProcessors());
			_externalCommunicationMock.Verify(l => l.StartInvoke(new InvokeData
																 {
																		 InvokeId = "invoke_id",
																		 InvokeUniqueId = invokeUniqueId,
																		 Type = new Uri("proto://type"),
																		 Source = new Uri("proto://src"),
																		 RawContent = "content",
																		 Content = DataModelValue.FromObject("content"),
																		 Parameters = default,
																 }, default));
			_externalCommunicationMock.Verify(l => l.CancelInvoke("invoke_id", default));
			_externalCommunicationMock.Verify(l => l.IsInvokeActive("invoke_id", invokeUniqueId));
			_externalCommunicationMock.VerifyNoOtherCalls();

			_loggerMock.Verify(l => l.Log(It.IsAny<string>(), null, "FinalizeExecuted", default, default));
			_loggerMock.VerifyGet(l => l.IsTracingEnabled);
			_loggerMock.VerifyNoOtherCalls();
		}
	}
}