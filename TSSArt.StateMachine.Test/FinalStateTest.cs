using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class FinalStateTest
	{
		[TestMethod]
		public async Task Final_state_with_number_as_done_data_Should_return_same_value()
		{
			// Arrange
			var stateMachine = new StateMachineFluentBuilder(BuilderFactory.Instance)
							   .BeginFinal()
							   .SetDoneData(ctx => new DataModelValue(22))
							   .EndFinal()
							   .Build();

			await using var stateMachineHost = new StateMachineHost(new StateMachineHostOptions());

			await stateMachineHost.StartAsync();

			// Act
			var result = await stateMachineHost.Execute(stateMachine);

			//Assert
			Assert.AreEqual(expected: 22, result.AsNumber());
		}

		[TestMethod]
		public async Task Input_argument_Should_be_passed_as_return_value()
		{
			// Arrange
			var stateMachine = new StateMachineFluentBuilder(BuilderFactory.Instance)
							   .BeginFinal()
							   .SetDoneData(ctx =>
											{
												dynamic data = ctx.DataModel;
												var val = (int) data._x.args;
												return new DataModelValue(val);
											})
							   .EndFinal()
							   .Build();

			await using var stateMachineHost = new StateMachineHost(new StateMachineHostOptions());

			await stateMachineHost.StartAsync();

			// Act
			var result = await stateMachineHost.Execute(stateMachine, new DataModelValue(33));

			//Assert
			Assert.AreEqual(expected: 33, result.AsNumber());
		}
	}
}