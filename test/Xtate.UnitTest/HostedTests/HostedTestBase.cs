#region Copyright © 2019-2020 Sergii Artemenko
// 
// This file is part of the Xtate project. <http://xtate.net>
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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Annotations;
using Xtate.CustomAction;

namespace Xtate.Test.HostedTests
{
	[SuppressMessage(category: "ReSharper", checkId: "RedundantCapturedContext")]
	public abstract class HostedTestBase
	{
		protected StateMachineHost Host   { get; private set; } = default!;
		protected Mock<ILogger>    Logger { get; private set; } = default!;

		[TestInitialize]
		public Task Initialize()
		{
			Logger = new Mock<ILogger>();

			Host = new StateMachineHostBuilder()
				   .AddCustomActionFactory(SystemActionFactory.Instance)
				   .AddResourceLoader(ResxResourceLoader.Instance)
				   .SetLogger(Logger.Object)
				   .Build();
			return Host.StartHostAsync();
		}

		[TestCleanup]
		public Task Cleanup() => Host.StopHostAsync();

		protected async Task Execute([PathReference("~/HostedTests/Scxml/")]
									 string scxmlPath)
		{
			var name = Assembly.GetExecutingAssembly().GetName().Name;
			await Host.ExecuteStateMachineAsync(new Uri($"resx://{name}/{name}/HostedTests/Scxml/" + scxmlPath));
		}
	}
}