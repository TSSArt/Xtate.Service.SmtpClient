using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xtate.Test
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			new StateMachineHostBuilder().AddAll().Build();
		}
	}
}