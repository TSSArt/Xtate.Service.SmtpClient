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
			var stateMachine = new StateMachineFluentBuilder(new BuilderFactory())
							   .BeginFinal()
							   .SetDoneData(ctx => new DataModelValue(22))
							   .EndFinal()
							   .Build();

			await using var ioProcessor = new IoProcessor(new IoProcessorOptions());

			// Act
			var result = await ioProcessor.Execute(stateMachine);

			//Assert
			Assert.AreEqual(expected: 22, result.AsNumber());
		}

		[TestMethod]
		public async Task Input_argument_Should_be_passed_as_return_value()
		{
			// Arrange
			var stateMachine = new StateMachineFluentBuilder(new BuilderFactory())
							   .BeginFinal()
							   .SetDoneData(ctx =>
											{
												dynamic data = ctx.DataModel;
												var val = (int) data._x.args;
												return new DataModelValue(val);
											})
							   .EndFinal()
							   .Build();

			await using var ioProcessor = new IoProcessor(new IoProcessorOptions());

			// Act
			var result = await ioProcessor.Execute(stateMachine, new DataModelValue(33));

			//Assert
			Assert.AreEqual(expected: 33, result.AsNumber());
		}
	}
}