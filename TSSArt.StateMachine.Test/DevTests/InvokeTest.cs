using System;
using System.Collections.Immutable;
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
		private Mock<IExternalCommunication> _externalCommunicationMock = default!;
		private Mock<ILogger>                _loggerMock                = default!;
		private InterpreterOptions           _options;
		private StateMachineEntity           _stateMachine;

		[TestInitialize]
		public void Initialize()
		{
			_stateMachine = new StateMachineEntity
							{
									States = ImmutableArray.Create<IStateEntity>(new StateEntity
																				 {
																						 Id = (Identifier) "S1",
																						 Invoke = ImmutableArray.Create<IInvoke>(new InvokeEntity
																																 {
																																		 Id = "invoke_id",
																																		 Source = new Uri("proto://src"),
																																		 Type = new Uri("proto://type"),
																																		 Content = new ContentEntity
																																				   {
																																						   Body =
																																								   new ContentBody
																																								   { Value = "content" }
																																				   },
																																		 Finalize = new FinalizeEntity
																																					{
																																							Action =
																																									ImmutableArray
																																											.Create<IExecutableEntity>(
																																													new LogEntity
																																													{
																																															Label =
																																																	"FinalizeExecuted"
																																													})
																																					}
																																 }),
																						 Transitions = ImmutableArray.Create<ITransition>(new TransitionEntity
																																		  {
																																				  EventDescriptors =
																																						  ImmutableArray.Create<IEventDescriptor>(
																																								  (EventDescriptor) "ToF"),
																																				  Target = ImmutableArray.Create<IIdentifier>(
																																						  (Identifier) "F")
																																		  })
																				 },
																				 new FinalEntity
																				 {
																						 Id = (Identifier) "F"
																				 })
							};

			_externalCommunicationMock = new Mock<IExternalCommunication>();
			_externalCommunicationMock.Setup(e => e.GetIoProcessors()).Returns(ImmutableArray<IIoProcessor>.Empty);
			_externalCommunicationMock.Setup(e => e.IsInvokeActive(It.IsAny<InvokeId>())).Returns(true);
			_loggerMock = new Mock<ILogger>();

			_options = new InterpreterOptions
					   {
							   ExternalCommunication = _externalCommunicationMock.Object,
							   Logger = _loggerMock.Object
					   };
		}

		private static EventObject CreateEventObject(string name, InvokeId? invokeId = null) => new EventObject(EventType.External, EventName.ToParts(name), data: default, sendId: default, invokeId);

		[TestMethod]
		public async Task SimpleTest()
		{
			var invokeUniqueId = "";
			_externalCommunicationMock.Setup(l => l.StartInvoke(It.IsAny<InvokeData>(), default))
									  .Callback((InvokeData data, CancellationToken token) => invokeUniqueId = data.InvokeId.InvokeUniqueIdValue);

			var channel = Channel.CreateUnbounded<IEvent>();
			var task = StateMachineInterpreter.RunAsync(SessionId.FromString("session1"), _stateMachine, channel, _options);
			await channel.Writer.WriteAsync(CreateEventObject(name: "fromInvoked", InvokeId.FromString(invokeId: "invoke_id", invokeUniqueId)));
			await channel.Writer.WriteAsync(CreateEventObject("ToF"));
			await task;

			_externalCommunicationMock.Verify(l => l.StartInvoke(It.IsAny<InvokeData>(), default));
			_externalCommunicationMock.Verify(l => l.CancelInvoke(InvokeId.FromString("invoke_id", invokeUniqueId), default));
			_externalCommunicationMock.Verify(l => l.IsInvokeActive(InvokeId.FromString("invoke_id", invokeUniqueId)));
			_externalCommunicationMock.VerifyNoOtherCalls();

			_loggerMock.Verify(l => l.LogInfo(It.IsAny<SessionId>(), null, "FinalizeExecuted", default, default));
			_loggerMock.VerifyGet(l => l.IsTracingEnabled);
			_loggerMock.VerifyNoOtherCalls();
		}
	}
}