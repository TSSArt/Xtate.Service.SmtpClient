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

using System;
using System.Threading.Tasks;
using Serilog;
using Xtate.Core;
using Xtate.IoC;

namespace Xtate;

[PublicAPI]
public static class LoggerSerilogExtensions
{
	public static void RegisterSerilogLogger(this IServiceCollection services)
	{
		services.RegisterSerilogLogger(configuration => configuration.WriteTo.Console());
	}

	public static void RegisterSerilogLogger(this IServiceCollection services, Action<LoggerConfiguration> options)
	{
		if (services.IsRegistered<SerilogLogWriter, string>())
		{
			return;
		}

		services.AddTransient<ILogWriter, Type>((provider, source) => LogWriterFactory(source, options));
	}

	private static ValueTask<ILogWriter> LogWriterFactory(Type source, Action<LoggerConfiguration> options)
	{
		var configuration = new LoggerConfiguration();
		options(configuration);

		var logWriter = new SerilogLogWriter(source, configuration);

		return new ValueTask<ILogWriter>(logWriter);
	}
}