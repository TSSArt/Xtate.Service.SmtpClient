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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;
using Xtate.Builder;

namespace Xtate.Core.Host
{
	internal static class Program
	{
		[STAThread]
		public static async Task Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var baseUri = new Uri(args.Length > 0 ? args[0] : "http://localhost:5000/");

			var stateMachineHost = new StateMachineHostBuilder()
								   .AddXPath()
								   .AddEcmaScript()
								   .AddHttpClient()
								   .AddSmtpClient()
								   .AddResourceLoaderFactory(ResxResourceLoaderFactory.Instance)
								   .AddResourceLoaderFactory(FileResourceLoaderFactory.Instance)
								   .AddResourceLoaderFactory(WebResourceLoaderFactory.Instance)
								   .AddCefSharpWebBrowser()
								   .AddUserInteraction()
								   .AddHttpIoProcessor(baseUri)
								   .SetSerilogLogger(cfg => cfg.MinimumLevel.Verbose().WriteTo.Console().WriteTo.Seq("http://beast:5341/"))
								   .Build();

			await stateMachineHost.StartHostAsync().ConfigureAwait(false);

			var name = Assembly.GetExecutingAssembly().GetName().Name;
			var autorun = stateMachineHost.ExecuteStateMachineAsync(new Uri($"resx://{name}/{name}/Scxml/autorun.scxml"));

			var sss = new StateMachineFluentBuilder(BuilderFactory.Instance).BeginState()
																			.BeginTransition()
																			.SetEvent(ImmutableArray.Create((IEventDescriptor) (EventDescriptor) "*"))
																			.AddOnTransition(Action)
																			.EndTransition()
																			.EndState()
																			.Build();

			var _ = stateMachineHost.ExecuteStateMachineAsync(sss, sessionId: "_system");

			Application.Run();

			await stateMachineHost.StopHostAsync().ConfigureAwait(false);
			await autorun.ConfigureAwait(false);
		}

		private static void Action(IExecutionContext executionContext)
		{
			//var host = (StateMachineHost) executionContext.RuntimeItems[typeof(IHost)];

			//host.
		}
	}
}