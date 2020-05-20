using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Xtate.Test.HostedTests
{
	[TestClass]
	public class CustomActionTest : HostedTestBase
	{
		[TestMethod]
		public async Task StartSystemAction()
		{
			// act
			await Execute("StartSystemAction.scxml");
			await Host.WaitAllStateMachinesAsync();

			// assert
			Logger.Verify(l => l.ExecuteLog(It.IsAny<ILoggerContext>(), "StartSystemActionChild", default, It.IsAny<CancellationToken>()));
		}
	}
}