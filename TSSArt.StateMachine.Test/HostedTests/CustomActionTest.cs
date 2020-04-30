using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace TSSArt.StateMachine.Test.HostedTests
{
	[TestClass]
	public class CustomActionTest : HostedTestBase
	{
		[TestMethod]
		public async Task StartSystemAction()
		{
			// act
			await Execute("StartSystemAction.scxml");
			await Host.WaitAllAsync();

			// assert
			Logger.Verify(l => l.LogInfo(It.IsAny<string>(), "StartSystemActionChild", "StartSystemActionChild", default, It.IsAny<CancellationToken>()));
		}
	}
}
