#region Copyright © 2019-2021 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
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

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.Core;
using Xtate.DataModel;
using Xtate.DataModel.EcmaScript;
using Xtate.IoC;
using Xtate.Service;

namespace Xtate.Test
{
	public class PassthroughFactoryService : ServiceFactoryBase
	{
		public required Func<DataModelValue, ValueTask<PassthroughService>> PassthroughServiceFactory { private get; init; }

		protected override void                Register(IServiceCatalog catalog) => catalog.Register(type: "passthrough", Creator);

		private async ValueTask<IService> Creator(ServiceLocator servicelocator,
											Uri? baseuri,
											InvokeData invokedata,
											IServiceCommunication servicecommunication,
											CancellationToken token)
		{
			return await PassthroughServiceFactory(invokedata.Parameters);
		}
	}

	public class PassthroughService : IService
	{
		private readonly DataModelValue _parameters;
		
		public PassthroughService(DataModelValue parameters) => _parameters = parameters;

		//protected override ValueTask<DataModelValue> Execute() => new(_parameters);

		public ValueTask Destroy(CancellationToken token) => default;

		public ValueTask<DataModelValue> GetResult(CancellationToken token) => new(_parameters);

		public ValueTask Send(IEvent evt, CancellationToken token) => default;
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
		<param name='obj' expr=""({key: 'value'})""/>
    </invoke>
    <transition event='done.invoke' target='final'/>
</state>
<final id='final'>
	<donedata><content expr='_event.data'/></donedata>
</final>
<final id='finErr'></final>";

			var stateMachine = StateMachineGenerator.FromInnerScxml_EcmaScript(scxml);

			/*var options = StateMachineHostOptionsTestBuilder.Create(o =>
																	{
																		o.ServiceFactories = ImmutableArray.Create(PassthroughFactoryService.Instance);
																	});*/


			//await using var stateMachineHost = new StateMachineHost(options) {_dataConverter = new DataConverter(null)};


			var sc1 = new ServiceCollection();
			sc1.RegisterStateMachineHost();
			sc1.AddImplementation<TraceLogWriter>().For<ILogWriter>();
			sc1.AddImplementation<PassthroughFactoryService>().For<IServiceFactory>();
			sc1.AddType<PassthroughService, DataModelValue>();
			sc1.RegisterEcmaScriptDataModelHandler();
			var sp1 = sc1.BuildProvider();

			var stateMachineHost = await sp1.GetRequiredService<StateMachineHost>();

			await stateMachineHost.StartHostAsync();

			// Act
			dynamic result = await stateMachineHost.ExecuteStateMachineAsync(stateMachine);

			//Assert
			Assert.AreEqual("value1", result.str);
			Assert.AreEqual(11, result.@int);
			Assert.AreEqual(3, result.arr.GetLength());
			Assert.AreEqual(1, result.arr[0]);
			Assert.AreEqual(2.5, result.arr[1]);
			Assert.AreEqual("3", result["arr"][2]);
			Assert.AreEqual("value", result.obj.key);
		}
	}
}