using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.Builder;

namespace Xtate.Test
{
	[TestClass]
	[SuppressMessage(category: "ReSharper", checkId: "RedundantCapturedContext")]
	public class StateMachineFluentBuilderTest
	{
		[TestMethod]
		public void EmptyStateMachineTest()
		{
			var builder = new StateMachineFluentBuilder(BuilderFactory.Instance);

			var stateMachine = builder.Build();

			Assert.IsNotNull(stateMachine);
			Assert.IsTrue(stateMachine.States.IsDefault);
		}

		[TestMethod]
		public void InitialAttributeStateMachineTest()
		{
			var builder = new StateMachineFluentBuilder(BuilderFactory.Instance);

			builder
					.SetInitial((Identifier) "S1")
					.BeginState((Identifier) "S1").EndState();

			var stateMachine = builder.Build();

			Assert.IsNotNull(stateMachine);
			Assert.AreEqual(expected: 1, stateMachine.States.Length);
			Assert.IsInstanceOfType(stateMachine.Initial, typeof(IInitial));
			Assert.IsInstanceOfType(stateMachine.States[0], typeof(IState));
			Assert.AreEqual((Identifier) "S1", ((IState) stateMachine.States[0]).Id);
		}

		[TestMethod]
		public void RootStatesStateMachineTest()
		{
			var builder = new StateMachineFluentBuilder(BuilderFactory.Instance);

			builder
					.BeginState((Identifier) "S1").EndState()
					.BeginParallel((Identifier) "P1").EndParallel()
					.BeginFinal((Identifier) "F1").EndFinal();

			var stateMachine = builder.Build();

			Assert.IsNotNull(stateMachine);
			Assert.AreEqual(expected: 3, stateMachine.States.Length);
			Assert.IsInstanceOfType(stateMachine.States[0], typeof(IState));
			Assert.IsInstanceOfType(stateMachine.States[1], typeof(IParallel));
			Assert.IsInstanceOfType(stateMachine.States[2], typeof(IFinal));
			Assert.AreEqual((Identifier) "S1", ((IState) stateMachine.States[0]).Id);
			Assert.AreEqual((Identifier) "P1", ((IParallel) stateMachine.States[1]).Id);
			Assert.AreEqual((Identifier) "F1", ((IFinal) stateMachine.States[2]).Id);
		}

		[TestMethod]
		public async Task DataModelTest()
		{
			var builder = new StateMachineFluentBuilder(BuilderFactory.Instance);

			builder
					.BeginState((Identifier) "S1")
					.AddOnEntry(ctx => ctx.DataModel["Hello"] = new DataModelValue("World"))
					.EndState();

			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			var eventChannel = channel.Reader;

			try
			{
				await StateMachineInterpreter.RunAsync(SessionId.New(), builder.Build(), eventChannel);

				Assert.Fail("StateMachineQueueClosedException should be raised");
			}
			catch (StateMachineQueueClosedException)
			{
				// ignore
			}
		}

		[TestMethod]
		public Task LiveLockErrorConditionTest()
		{
			var builder = new StateMachineFluentBuilder(BuilderFactory.Instance);

			builder
					.BeginState((Identifier) "S1")
					.BeginTransition()
					.SetCondition(context => throw new InvalidOperationException("some exception"))
					.EndTransition()
					.EndState();

			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			var eventChannel = channel.Reader;

			async Task AssertAction()
			{
				var options = new InterpreterOptions { UnhandledErrorBehaviour = UnhandledErrorBehaviour.IgnoreError };
				await StateMachineInterpreter.RunAsync(SessionId.New(), builder.Build(), eventChannel, options);
			}

			return Assert.ThrowsExceptionAsync<StateMachineDestroyedException>(AssertAction);
		}

		[TestMethod]
		public Task LiveLockPingPongTest()
		{
			var builder = new StateMachineFluentBuilder(BuilderFactory.Instance);

			builder
					.BeginState((Identifier) "S1")
					.BeginTransition()
					.SetTarget((Identifier) "S2")
					.EndTransition()
					.EndState()
					.BeginState((Identifier) "S2")
					.BeginTransition()
					.SetTarget((Identifier) "S1")
					.EndTransition()
					.EndState();

			var channel = Channel.CreateUnbounded<IEvent>();
			channel.Writer.Complete();
			var eventChannel = channel.Reader;

			async Task AssertAction() => await StateMachineInterpreter.RunAsync(SessionId.New(), builder.Build(), eventChannel);

			return Assert.ThrowsExceptionAsync<StateMachineDestroyedException>(AssertAction);
		}
	}
}