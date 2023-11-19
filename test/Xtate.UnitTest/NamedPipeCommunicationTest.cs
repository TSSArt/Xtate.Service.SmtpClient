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
using Xtate.IoC;
using Xtate.IoProcessor;

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
			var serviceLocator = ServiceLocator.Create(
				delegate (IServiceCollection s)
				{
					s.AddXPath();
					s.AddEcmaScript();
				});
			
			//var srcPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "me", U("src")).Build(serviceLocator);

			var pipeFactory = new NamedPipeIoProcessorFactory("me", U("src"));
			var fileLogWriter = new TraceLogWriter();

			var sc1 = new ServiceCollection();
			sc1.RegisterStateMachineHost();
			sc1.AddForwarding<ILogWriter>(_ => fileLogWriter);
			sc1.RegisterEcmaScriptDataModelHandler();
			sc1.AddForwarding<IIoProcessorFactory>(_ => pipeFactory);
			var sp1 = sc1.BuildProvider();

			var srcPrc = await sp1.GetRequiredService<StateMachineHost>();

			await srcPrc.StartHostAsync();
			await srcPrc.StartStateMachineAsync(string.Format(SrcScxml, $"named.pipe:///{U("src")}#_session_dstID"), sessionId: "srcID");
			var dst = srcPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID").Preserve();

			if (await srcPrc.TryGetEventDispatcher(SessionId.FromString("srcID"), token: default) is { } eventDispatcher)
			{
				await eventDispatcher.Send(CreateEventObject("trigger"), default);
			}

			var result = await dst;

			await srcPrc.WaitAllStateMachinesAsync();

			await srcPrc.StopHostAsync();

			Assert.AreEqual($"http://www.w3.org/TR/scxml/#NamedPipeEventProcessor+named.pipe://me/{U("src")}#_session_srcID", result.AsString());
		}

		[TestMethod]
		public async Task SameAppDomainNoPipesTest()
		{
			var serviceLocator = ServiceLocator.Create(
				delegate(IServiceCollection s)
				{
					s.AddXPath();
					s.AddEcmaScript();
				});

			var fileLogWriter = new FileLogWriter("D:\\Ser\\sss1.txt");
			var srcPipeFactory = new NamedPipeIoProcessorFactory("me", U("src"));
			var dstPipeFactory = new NamedPipeIoProcessorFactory("me", U("dst"));
			var sc1 = new ServiceCollection();
			sc1.RegisterStateMachineHost();
			sc1.RegisterEcmaScriptDataModelHandler();
			//sc1.AddForwarding<ILogWriter>(_ => fileLogWriter);
			sc1.AddForwarding<IIoProcessorFactory>(_ => srcPipeFactory);
			var sp1 = sc1.BuildProvider();

			var sc2 = new ServiceCollection();
			sc2.RegisterStateMachineHost();
			sc2.AddForwarding<IIoProcessorFactory>(_ => dstPipeFactory);
			sc2.RegisterEcmaScriptDataModelHandler();
			//sc2.AddForwarding<ILogWriter>(_ => fileLogWriter);
			var sp2 = sc2.BuildProvider();

			//var srcPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "me", U("src")).Build(serviceLocator);
			//var dstPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "me", U("dst")).Build(serviceLocator);

			var srcPrc = await sp1.GetRequiredService<StateMachineHost>();
			var dstPrc = await sp2.GetRequiredService<StateMachineHost>();

			await srcPrc.StartHostAsync();
			await dstPrc.StartHostAsync();

			await srcPrc.StartStateMachineAsync(string.Format(SrcScxml, $"iop:///{U("dst")}#_session_dstID"), sessionId: "srcID");
			var dst = dstPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID").Preserve();

			if (await srcPrc.TryGetEventDispatcher(SessionId.FromString("srcID"), token: default) is { } eventDispatcher)
			{
				await eventDispatcher.Send(CreateEventObject("trigger"), default);
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
			var serviceLocator = ServiceLocator.Create(
				delegate (IServiceCollection s)
				{
					s.AddXPath();
					s.AddEcmaScript();
				});

			var fileLogWriter = new FileLogWriter("D:\\Ser\\sss2.txt");
			var srcPipeFactory = new NamedPipeIoProcessorFactory("MyHost1", U("src"));
			var dstPipeFactory = new NamedPipeIoProcessorFactory(".", U("dst"));
			var sc1 = new ServiceCollection();
			sc1.RegisterStateMachineHost();
			sc1.RegisterEcmaScriptDataModelHandler();
			//sc1.AddForwarding<ILogWriter>(_ => fileLogWriter);
			sc1.AddForwarding<IIoProcessorFactory>(_ => srcPipeFactory);
			var sp1 = sc1.BuildProvider();

			var sc2 = new ServiceCollection();
			sc2.RegisterStateMachineHost();
			sc2.AddForwarding<IIoProcessorFactory>(_ => dstPipeFactory);
			sc2.RegisterEcmaScriptDataModelHandler();
			//sc2.AddForwarding<ILogWriter>(_ => fileLogWriter);
			var sp2 = sc2.BuildProvider();

			//var srcPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: "MyHost1", U("src")).Build(serviceLocator);
			//var dstPrc = new StateMachineHostBuilder().AddNamedPipeIoProcessor(host: ".", U("dst")).Build(serviceLocator);

			var srcPrc = await sp1.GetRequiredService<StateMachineHost>();
			var dstPrc = await sp2.GetRequiredService<StateMachineHost>();


			await srcPrc.StartHostAsync();
			await dstPrc.StartHostAsync();

			await srcPrc.StartStateMachineAsync(string.Format(SrcScxml, $"named.pipe://./{U("dst")}#_session_dstID"), sessionId: "srcID");
			var dst = dstPrc.ExecuteStateMachineAsync(DstScxml, sessionId: "dstID").Preserve();

			if (await srcPrc.TryGetEventDispatcher(SessionId.FromString("srcID"), token: default) is { } eventDispatcher)
			{
				await eventDispatcher.Send(CreateEventObject("trigger"), default);
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