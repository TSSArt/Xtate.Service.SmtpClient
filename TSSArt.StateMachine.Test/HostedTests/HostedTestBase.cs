using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine.Test.HostedTests
{
	public abstract class HostedTestBase
	{
		protected StateMachineHost Host { get; private set; }
		protected Mock<ILogger> Logger { get; private set; }

		[TestInitialize]
		public async Task Initialize()
		{
			Logger = new Mock<ILogger>();

			Host = new StateMachineHostBuilder()
				   .AddCustomActionFactory(SystemActionFactory.Instance)
				   .AddResourceLoader(ResxResourceLoader.Instance)
				   .SetLogger(Logger.Object)
				   .Build();
			await Host.StartAsync();
		}

		[TestCleanup]
		public async Task Cleanup()
		{
			await Host.StopAsync();
		}

		protected async Task Execute([PathReference("~/HostedTests/Scxml/")] string scxmlPath)
		{
			var name = Assembly.GetExecutingAssembly().GetName().Name;
			await Host.Execute(new Uri($"resx://{name}/{name}/HostedTests/Scxml/" + scxmlPath));
		}
	}
}
