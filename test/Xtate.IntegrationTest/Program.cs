#region Copyright © 2019-2020 Sergii Artemenko
// 
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
// 
#endregion

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xtate.CustomAction;
using Xtate.Service;

namespace Xtate.IntegrationTest
{
	internal static class Program
	{
		private static readonly Uri ScxmlBase = new Uri("res://Xtate.IntegrationTest/Xtate.IntegrationTest/Scxml/");

		private static async Task Main(string[] args)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());

			await using var stateMachineHost = new StateMachineHostBuilder()
											   .AddEcmaScript()
											   .AddHttpIoProcessor(new Uri(args.Length > 0 ? args[0] : "http://localhost:5001/"))
											   .AddServiceFactory(HttpClientService.Factory)
											   .AddServiceFactory(SmtpClientService.Factory)
											   .AddCustomActionFactory(BasicCustomActionFactory.Instance)
											   .AddCustomActionFactory(MimeCustomActionFactory.Instance)
											   .AddCustomActionFactory(MidCustomActionFactory.Instance)
											   .AddResourceLoader(ResxResourceLoader.Instance)
											   .SetConfigurationValue(key: "uiEndpoint", value: "http://localhost:5000/dialog")
											   .SetConfigurationValue(key: "mailEndpoint", value: "http://mid.dev.tssart.com/MailServer/Web2/api/Mail/")
											   .Build();

			await stateMachineHost.StartHostAsync().ConfigureAwait(false);

			var prms = new
					   {
							   loginUrl = "https://test.tssart.com/wp-login.php",
							   username = "tadex1",
							   password = "123456"
					   };

			var task = stateMachineHost.ExecuteStateMachineAsync(new Uri(ScxmlBase, relativeUri: "signup.scxml"), DataModelValue.FromObject(prms));

			dynamic result = await task.ConfigureAwait(false);

			var prms2 = new { profileUrl = "https://test.tssart.com/wp-admin/profile.php", result.data.cookies };
			var task2 = stateMachineHost.ExecuteStateMachineAsync(new Uri(ScxmlBase, relativeUri: "captureEmail.scxml"), DataModelValue.FromObject(prms2));

			dynamic _ = await task2.ConfigureAwait(false);

			await stateMachineHost.StopHostAsync().ConfigureAwait(false);
		}
	}
}