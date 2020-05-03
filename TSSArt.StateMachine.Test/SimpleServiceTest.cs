using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
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

			await stateMachineHost.StartAsync();

			// Act
			dynamic result = await stateMachineHost.ExecuteAsync(stateMachine);

			//Assert
			Assert.AreEqual("value1", result.str);
			Assert.AreEqual(11, result.@int);
			Assert.AreEqual(3, result.arr.Length);
			Assert.AreEqual(1, result.arr[0]);
			Assert.AreEqual(2.5, result.arr[1]);
			Assert.AreEqual("3", result["arr"][2]);
			Assert.AreEqual("val", result.obj.key);
		}
	}
}