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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xtate.Core;
using Xtate.IoC;
using Xtate.Persistence;

namespace Xtate.CustomAction
{
	public class StorageActionService : IAsyncInitialization, IDisposable
	{
		private readonly DisposingToken _disposingToken = new();

		public required Func<(string? Partition, string Key), ValueTask<ITransactionalStorage>> TransactionalStorageFactory { private get; init; }

		private readonly AsyncInit<ITransactionalStorage> _storageAsyncInit;
		private readonly AsyncReaderWriterLock            _asyncReaderWriterLock = new();

		public StorageActionService() => _storageAsyncInit = AsyncInit.RunAfter(this, static sas => sas.TransactionalStorageFactory((@"mid", @"storage")));

		public Task Initialization => _storageAsyncInit.Task;

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_disposingToken.Dispose();
				_asyncReaderWriterLock.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public async ValueTask<DataModelValue> GetValue(string variable)
		{
			await _asyncReaderWriterLock.AcquireReaderLock(_disposingToken.Token).ConfigureAwait(false);

			try
			{
				return DataModelValueSerializer.Load(_storageAsyncInit.Value, variable);
			}
			finally
			{
				_asyncReaderWriterLock.ReleaseReaderLock();
			}
		}

		public async ValueTask SetValue(string variable, DataModelValue value)
		{
			await _asyncReaderWriterLock.AcquireWriterLock(_disposingToken.Token).ConfigureAwait(false);

			try
			{
				DataModelValueSerializer.Save(_storageAsyncInit.Value, variable, value);

				await _storageAsyncInit.Value.CheckPoint(level: 0).ConfigureAwait(false);
			}
			finally
			{
				_asyncReaderWriterLock.ReleaseWriterLock();
			}
		}

		[SuppressMessage(category: "Performance", checkId: "CA1822:Mark members as static")]
		[SuppressMessage(category: "ReSharper", checkId: "MemberCanBeMadeStatic.Global")]
		public string CreateValue(string lastValue, string? rule, string? template)
		{
			var query = EnumeratePredefinedValues(template, rule);

			if (lastValue.Length > 0)
			{
				query = query.SkipWhile(value => value != lastValue).Skip(1);
			}

			return query.FirstOrDefault() ?? CreateRandomValue(template, rule);
		}

		private static string CreateRandomValue(string? template, string? rule)
		{
			string result;

			foreach (var value in GetPredefinedValues(template))
			{
				if (TryGenerate(template, rule, value, index: 0, random: true, out result))
				{
					return result;
				}
			}

			if (TryGenerate(template, rule, string.Empty, index: 0, random: true, out result))
			{
				return result;
			}

			throw new InvalidOperationException(@"Can't generate value.");
		}

		private static IEnumerable<string> EnumeratePredefinedValues(string? template, string? rule)
		{
			foreach (var value in GetPredefinedValues(template))
			{
				if (TryGenerate(template, rule, value, index: 0, random: false, out var result))
				{
					yield return result;
				}
			}

			foreach (var value in GetPredefinedValues(template))
			{
				for (var i = 1; i < 9; i ++)
				{
					if (TryGenerate(template, rule, value, i, random: false, out var result))
					{
						yield return result;
					}
				}
			}
		}

		private static string[] GetPredefinedValues(string? template)
		{
			return template switch
				   {
					   "USERID" => new[] { "tadex", "xtadex" },
					   _        => Array.Empty<string>()
				   };
		}

		private static bool TryGenerate(string? template,
										string? rule,
										string value,
										int index,
										bool random,
										out string result)
		{
			result = value;

			if (index > 0)
			{
				result += index;
			}

			if (random)
			{
				var length = template == "PASSWORD" ? 16 : 2;
				for (var i = 0; i < length; i ++)
				{
					result += new Random().Next(minValue: 0, maxValue: 9);
				}
			}

			return rule is not null && Regex.IsMatch(result, rule);
		}
	}
}