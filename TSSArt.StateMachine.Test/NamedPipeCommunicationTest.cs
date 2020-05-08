using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
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

		private static string U(string v, [CallerMemberName] string? member = null) => v + "_" + member;

		private static EventObject CreateEventObject(string name) => new EventObject(EventType.External, EventName.ToParts(name));

		[TestMethod]
		public async Task SameStateMachineHostTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedIoProcessor(U("src")).AddEcmaScript().Build();
			await srcPrc.StartAsync();
			var _ = srcPrc.ExecuteAsync(string.Format(SrcScxml, $"iop:///{U("src")}#_scxml_dstID"), sessionId: "srcID");
			var dst = srcPrc.ExecuteAsync(DstScxml, sessionId: "dstID");

			await srcPrc.Dispatch(SessionId.FromString("srcID"), CreateEventObject("trigger"));

			var result = await dst;

			await srcPrc.WaitAllAsync();

			await srcPrc.StopAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe:///{U("src")}#_scxml_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainNoPipesTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedIoProcessor(U("src")).Build();
			await srcPrc.StartAsync();
			var _ = srcPrc.ExecuteAsync(string.Format(SrcScxml, $"iop:///{U("dst")}#_scxml_dstID"), sessionId: "srcID");

			var dstPrc = new StateMachineHostBuilder().AddNamedIoProcessor(U("dst")).AddEcmaScript().Build();
			await dstPrc.StartAsync();
			var dst = dstPrc.ExecuteAsync(DstScxml, sessionId: "dstID");


			await srcPrc.Dispatch(SessionId.FromString("srcID"), CreateEventObject("trigger"));

			var result = await dst;

			await srcPrc.WaitAllAsync();
			await dstPrc.WaitAllAsync();
			await srcPrc.StopAsync();
			await dstPrc.StopAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe:///{U("src")}#_scxml_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainPipesTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedIoProcessor(host: "MyHost1", U("src")).Build();
			await srcPrc.StartAsync();
			var _ = srcPrc.ExecuteAsync(string.Format(SrcScxml, $"iop://./{U("dst")}#_scxml_dstID"), sessionId: "srcID");

			var dstPrc = new StateMachineHostBuilder().AddNamedIoProcessor(host: ".", U("dst")).AddEcmaScript().Build();
			await dstPrc.StartAsync();
			var dst = dstPrc.ExecuteAsync(DstScxml, sessionId: "dstID");


			await srcPrc.Dispatch(SessionId.FromString("srcID"), CreateEventObject("trigger"));

			var result = await dst;

			await srcPrc.WaitAllAsync();
			await dstPrc.WaitAllAsync();
			await srcPrc.StopAsync();
			await dstPrc.StopAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe://myhost1/{U("src")}#_scxml_srcID", result.AsString());
		}
	}
}