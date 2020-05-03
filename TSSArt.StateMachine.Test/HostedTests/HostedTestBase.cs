using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine.Test.HostedTests
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
			return Host.StartAsync();
		}

		[TestCleanup]
		public Task Cleanup() => Host.StopAsync();

		protected async Task Execute([PathReference("~/HostedTests/Scxml/")]
									 string scxmlPath)
		{
			var name = Assembly.GetExecutingAssembly().GetName().Name;
			await Host.ExecuteAsync(new Uri($"resx://{name}/{name}/HostedTests/Scxml/" + scxmlPath));
		}
	}
}