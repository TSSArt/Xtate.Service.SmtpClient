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

		private static string U(string v, [CallerMemberName] string member = null) => v + "_" + member;

		[TestMethod]
		public async Task SameIoProcessorTest()
		{
			var srcPrc = new IoProcessorBuilder().AddNamedEventProcessor(U("src")).AddEcmaScript().Build();
			await srcPrc.StartAsync();
			var _ = srcPrc.Execute(sessionId: "srcID", string.Format(SrcScxml, $"iop:///{U("src")}#_scxml_dstID"));
			var dst = srcPrc.Execute(sessionId: "dstID", DstScxml);

			await srcPrc.Dispatch(sessionId: "srcID", new EventObject("trigger"));

			var result = await dst;

			await srcPrc.StopAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe:///{U("src")}#_scxml_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainNoPipesTest()
		{
			var srcPrc = new IoProcessorBuilder().AddNamedEventProcessor(U("src")).Build();
			await srcPrc.StartAsync();
			var _ = srcPrc.Execute(sessionId: "srcID", string.Format(SrcScxml, $"iop:///{U("dst")}#_scxml_dstID"));

			var dstPrc = new IoProcessorBuilder().AddNamedEventProcessor(U("dst")).AddEcmaScript().Build();
			await dstPrc.StartAsync();
			var dst = dstPrc.Execute(sessionId: "dstID", DstScxml);


			await srcPrc.Dispatch(sessionId: "srcID", new EventObject("trigger"));

			var result = await dst;

			await srcPrc.StopAsync();
			await dstPrc.StopAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe:///{U("src")}#_scxml_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainPipesTest()
		{
			var srcPrc = new IoProcessorBuilder().AddNamedEventProcessor(host: "MyHost1", U("src")).Build();
			await srcPrc.StartAsync();
			var _ = srcPrc.Execute(sessionId: "srcID", string.Format(SrcScxml, $"iop://./{U("dst")}#_scxml_dstID"));

			var dstPrc = new IoProcessorBuilder().AddNamedEventProcessor(host: ".", U("dst")).AddEcmaScript().Build();
			await dstPrc.StartAsync();
			var dst = dstPrc.Execute(sessionId: "dstID", DstScxml);


			await srcPrc.Dispatch(sessionId: "srcID", new EventObject("trigger"));

			var result = await dst;

			await srcPrc.StopAsync();
			await dstPrc.StopAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#SCXMLEventProcessor+pipe://myhost1/{U("src")}#_scxml_srcID", result.AsString());
		}
	}
}