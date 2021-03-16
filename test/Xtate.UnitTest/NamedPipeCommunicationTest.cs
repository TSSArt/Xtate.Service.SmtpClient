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

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xtate.Core;

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
		<send target='{0}' type='named.pipe' event='trigger2'/>
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

		private static EventObject CreateEventObject(string name) => new() { Type = EventType.External, NameParts = EventName.ToParts(name) };

		[TestMethod]
		public async Task SameStateMachineHostTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "me", U("src")).AddEcmaScript().Build();
			await srcPrc.StartHostAsync();
			await srcPrc.StartStateMachineAsync(string.Format(SrcScxml, $"named.pipe:///{U("src")}#_session_dstID"), sessionId: "srcID");
			var dst = srcPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID").Preserve();

			if (await srcPrc.TryGetEventDispatcher(SessionId.FromString("srcID"), token: default) is { } eventDispatcher)
			{
				await eventDispatcher.Send(CreateEventObject("trigger"));
			}

			var result = await dst;

			await srcPrc.WaitAllStateMachinesAsync();

			await srcPrc.StopHostAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#NamedPipeEventProcessor+named.pipe://me/{U("src")}#_session_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainNoPipesTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "me", U("src")).Build();
			var dstPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "me", U("dst")).AddEcmaScript().Build();

			await srcPrc.StartHostAsync();
			await dstPrc.StartHostAsync();

			await srcPrc.StartStateMachineAsync(string.Format(SrcScxml, $"iop:///{U("dst")}#_session_dstID"), sessionId: "srcID");
			var dst = dstPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID").Preserve();

			if (await srcPrc.TryGetEventDispatcher(SessionId.FromString("srcID"), token: default) is { } eventDispatcher)
			{
				await eventDispatcher.Send(CreateEventObject("trigger"));
			}

			var result = await dst;

			await srcPrc.WaitAllStateMachinesAsync();
			await dstPrc.WaitAllStateMachinesAsync();
			await srcPrc.StopHostAsync();
			await dstPrc.StopHostAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#NamedPipeEventProcessor+named.pipe://me/{U("src")}#_session_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainPipesTest()
		{
			var srcPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "MyHost1", U("src")).Build();
			var dstPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: ".", U("dst")).AddEcmaScript().Build();

			await srcPrc.StartHostAsync();
			await dstPrc.StartHostAsync();

			await srcPrc.StartStateMachineAsync(string.Format(SrcScxml, $"iop://./{U("dst")}#_session_dstID"), sessionId: "srcID");
			var dst = dstPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID").Preserve();

			if (await srcPrc.TryGetEventDispatcher(SessionId.FromString("srcID"), token: default) is { } eventDispatcher)
			{
				await eventDispatcher.Send(CreateEventObject("trigger"));
			}

			var result = await dst;

			await srcPrc.WaitAllStateMachinesAsync();
			await dstPrc.WaitAllStateMachinesAsync();
			await srcPrc.StopHostAsync();
			await dstPrc.StopHostAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#NamedPipeEventProcessor+named.pipe://myhost1/{U("src")}#_session_srcID", result.AsString());
		}
	}
}