#region Copyright © 2019-2021 Sergii Artemenko

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

#endregion

using System;
using Serilog;

namespace Xtate
{
	[PublicAPI]
	public static class LoggerSerilogExtensions
	{
		public static StateMachineHostBuilder SetSerilogLogger(this StateMachineHostBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			var configuration = new LoggerConfiguration().WriteTo.Console();

			builder.SetLogger(new SerilogLogger(configuration));

			return builder;
		}

		public static StateMachineHostBuilder SetSerilogLogger(this StateMachineHostBuilder builder, Action<LoggerConfiguration> options)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));
			if (options is null) throw new ArgumentNullException(nameof(options));

			var configuration = new LoggerConfiguration();

			options(configuration);

			builder.SetLogger(new SerilogLogger(configuration));

			return builder;
		}
	}
}