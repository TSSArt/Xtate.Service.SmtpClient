using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xtate.Test
{
	[TestClass]
	public class NamedPipeCommunicationTest
	{
		private const string SrcScxml = @"
<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0'>
<state>
    <transition event='trigger' target='final'/>
</state>
<final id='final'>
	<onentry>
		<send target='{0}' type='scxml' event='trigger2'/>
    </onentry>
</final>
</scxml>";

		private const string DstScxml = @"
<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>
<state>
    <transition event='trigger2' target='final'/>
</state>
<final id='final'>
	<donedata><content expr='_event.origintype + &quot;+&quot; + _event.origin'/></donedata>
</final>
</scxml>";

		private static string U(string v, [CallerMemberName] string? member = default) => v + "_" + member;

		private static EventObject CreateEventObject(string name) => new EventObject(EventType.External, EventName.ToParts(name));

		[TestMethod]
		public async Task SameStateMachineHostTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedIoProcessor(U("src")).AddEcmaScript().Build();
			await srcPrc.StartHostAsync();
			var _ = srcPrc.ExecuteStateMachineAsync(string.Format(SrcScxml, $"iop:///{U("src")}#_scxml_dstID"), sessionId: "srcID");
			var dst = srcPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID");

			await srcPrc.Dispatch(SessionId.FromString("srcID"), CreateEventObject("trigger"));

			var result = await dst;

			await srcPrc.WaitAllStateMachinesAsync();

			await srcPrc.StopHostAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe:///{U("src")}#_scxml_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainNoPipesTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedIoProcessor(U("src")).Build();
			await srcPrc.StartHostAsync();
			var _ = srcPrc.ExecuteStateMachineAsync(string.Format(SrcScxml, $"iop:///{U("dst")}#_scxml_dstID"), sessionId: "srcID");

			var dstPrc = new StateMachineHostBuilder().AddNamedIoProcessor(U("dst")).AddEcmaScript().Build();
			await dstPrc.StartHostAsync();
			var dst = dstPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID");


			await srcPrc.Dispatch(SessionId.FromString("srcID"), CreateEventObject("trigger"));

			var result = await dst;

			await srcPrc.WaitAllStateMachinesAsync();
			await dstPrc.WaitAllStateMachinesAsync();
			await srcPrc.StopHostAsync();
			await dstPrc.StopHostAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe:///{U("src")}#_scxml_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainPipesTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedIoProcessor(host: "MyHost1", U("src")).Build();
			await srcPrc.StartHostAsync();
			var _ = srcPrc.ExecuteStateMachineAsync(string.Format(SrcScxml, $"iop://./{U("dst")}#_scxml_dstID"), sessionId: "srcID");

			var dstPrc = new StateMachineHostBuilder().AddNamedIoProcessor(host: ".", U("dst")).AddEcmaScript().Build();
			await dstPrc.StartHostAsync();
			var dst = dstPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID");


			await srcPrc.Dispatch(SessionId.FromString("srcID"), CreateEventObject("trigger"));

			var result = await dst;

			await srcPrc.WaitAllStateMachinesAsync();
			await dstPrc.WaitAllStateMachinesAsync();
			await srcPrc.StopHostAsync();
			await dstPrc.StopHostAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe://myhost1/{U("src")}#_scxml_srcID", result.AsString());
		}
	}
}