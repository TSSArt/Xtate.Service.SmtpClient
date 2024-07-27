// Copyright © 2019-2024 Sergii Artemenko
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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Xtate.Core;
using Xtate.IoC;

namespace Xtate.Logger.Serilog.Test;

[TestClass]
public class SerilogTest
{
	[TestMethod]
	public async Task SimpleSerilogTest()
	{
		var services = new ServiceCollection();
		services.RegisterSerilogLogger(
			options => options
					   .MinimumLevel.Verbose()
					   .WriteTo.Console()
					   .WriteTo.Seq("http://127.0.0.1:5341"));
		services.RegisterStateMachineInterpreter();
		services.AddShared<IStateMachine>(
			SharedWithin.Container, _ => new StateMachineEntity
										 {
											 Name = "MyName",
											 States =
											 [
												 new FinalEntity
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