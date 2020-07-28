#region Copyright © 2019-2020 Sergii Artemenko
// 
// This file is part of the Xtate project. <http://xtate.net>
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
// 
#endregion

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.DataModel.EcmaScript;
using Xtate.Service;

namespace Xtate.Test
{
	[SimpleService("passthrough")]
	public class PassthroughService : SimpleServiceBase
	{
		protected override ValueTask<DataModelValue> Execute() => new ValueTask<DataModelValue>(Parameters);
	}

	[TestClass]
	public class SimpleServiceTest
	{
		[TestMethod]
		public async Task Input_invoke_parameters_Should_be_passed_to_service()
		{
			// Arrange
			const string scxml = @"
<state>
    <invoke type='passthrough'>
		<param name='str' expr=""'value1'""/>
		<param name='int' expr=""11""/>
		<param name='arr' expr=""[1, 2.5, '3']""/>
		<param name='obj' expr=""({key: 'val'})""/>
    </invoke>
    <transition event='done.invoke' target='final'/>
</state>
<final id='final'>
	<donedata><content expr='_event.data'/></donedata>
</final>
<final id='finErr'></final>";

			var stateMachine = StateMachineGenerator.FromInnerScxml_EcmaScript(scxml);

			var options = StateMachineHostOptionsTestBuilder.Create(o =>
																	{
																		o.DataModelHandlerFactories = ImmutableArray.Create(EcmaScriptDataModelHandler.Factory);
																		o.ServiceFactories = ImmutableArray.Create(SimpleServiceFactory<PassthroughService>.Instance);
																	});

			await using var stateMachineHost = new StateMachineHost(options);

			await stateMachineHost.StartHostAsync();

			// Act
			dynamic result = await stateMachineHost.ExecuteStateMachineAsync(stateMachine);

			//Assert
			Assert.AreEqual("value1", result.str);
			Assert.AreEqual(11, result.@int);
			Assert.AreEqual(3, result.arr.length);
			Assert.AreEqual(1, result.arr[0]);
			Assert.AreEqual(2.5, result.arr[1]);
			Assert.AreEqual("3", result["arr"][2]);
			Assert.AreEqual("val", result.obj.key);
		}
	}
}