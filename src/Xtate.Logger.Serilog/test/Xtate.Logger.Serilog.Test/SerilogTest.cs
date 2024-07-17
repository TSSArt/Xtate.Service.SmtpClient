using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Events;
using Xtate.Core;
using Xtate.IoC;

namespace Xtate.Logger.Serilog.Test
{
	[TestClass]
    public class SerilogTest
    {
		[TestMethod]
		public async Task SimpleSerilogTest()
		{
			var services = new ServiceCollection();
			services.RegisterSerilogLogger(options => options
													  .MinimumLevel.Verbose()
													  .WriteTo.Console()
													  .WriteTo.Seq("http://127.0.0.1:5341"));
			services.RegisterStateMachineInterpreter();
			services.AddShared<IStateMachine>(SharedWithin.Container, _ => new StateMachineEntity()
																		   {
																			   Name = "MyName",
																			   States = [
																				   new FinalEntity()
																				   {
																					   Id = Identifier.FromString("Id1")
																				   }
																			   ]
																		   });
			await using var provider = services.BuildProvider();

			var smi = await provider.GetRequiredService<IStateMachineInterpreter>();

			await smi.RunAsync();
		}
    }

}
