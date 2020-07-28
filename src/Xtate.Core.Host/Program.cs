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

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

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

			await using var stateMachineHost = new StateMachineHostBuilder()
											   .AddEcmaScript()
											   .AddCefSharpWebBrowser()
											   .AddUserInteraction()
											   .AddHttpIoProcessor(baseUri)
											   .DisableVerboseValidation()
											   .AddResourceLoader(ResxResourceLoader.Instance)
											   .SetSerilogLogger()
											   .Build();

			await stateMachineHost.StartHostAsync().ConfigureAwait(false);

			var name = Assembly.GetExecutingAssembly().GetName().Name;
			var autorun = stateMachineHost.ExecuteStateMachineAsync(new Uri($"resx://{name}/{name}/Scxml/autorun.scxml"));

			Application.Run();

			await stateMachineHost.StopHostAsync().ConfigureAwait(false);
			await autorun.ConfigureAwait(false);
		}
	}
}