#region Copyright © 2019-2020 Sergii Artemenko
// This file is part of the Xtate project. <http://xtate.net>
// Copyright © 2019-2020 Sergii Artemenko
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.Builder;

namespace Xtate.Test
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

			await stateMachineHost.StartHostAsync();

			// Act
			var result = await stateMachineHost.ExecuteStateMachineAsync(stateMachine);

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

			await stateMachineHost.StartHostAsync();

			// Act
			var result = await stateMachineHost.ExecuteStateMachineAsync(stateMachine, new DataModelValue(33));

			//Assert
			Assert.AreEqual(expected: 33, result.AsNumber());
		}
	}
}