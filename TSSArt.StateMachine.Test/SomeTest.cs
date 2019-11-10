using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class SomeTest
	{
		[TestMethod]
		public async Task Final_state_with_number_as_done_data_Should_return_same_value()
		{
			// Arrange
			var stateMachine = new StateMachineFluentBuilder(new BuilderFactory())
							   .BeginFinal("F")
							   .SetDoneData(context => new DataModelValue(22))
							   .EndFinal()
							   .Build();

			await using var ioProcessor = new IoProcessor(new IoProcessorOptions());
			
			// Act
			var result = await ioProcessor.Execute(stateMachine);

			//Assert
			Assert.AreEqual(expected: 22, result.AsNumber());
		}
	}
}