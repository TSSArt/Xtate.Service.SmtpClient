#region Copyright © 2019-2020 Sergii Artemenko

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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xtate.Persistence;

namespace Xtate.CustomAction
{
	internal sealed class StorageActionService : IDisposable
	{
		private readonly AsyncReaderWriterLock _asyncReaderWriterLock = new AsyncReaderWriterLock();

		private readonly Lazy<Task<ITransactionalStorage>> StorageTask = new Lazy<Task<ITransactionalStorage>>(GetStorage, LazyThreadSafetyMode.ExecutionAndPublication);

	#region Interface IDisposable

		public void Dispose() => _asyncReaderWriterLock.Dispose();

	#endregion

		private static async Task<ITransactionalStorage> GetStorage() =>
				await new FileStorageProvider(path: "dir").GetTransactionalStorage(partition: null, key: "default", token: default).ConfigureAwait(false);

		public async ValueTask<DataModelValue> GetValue(string variable, CancellationToken token)
		{
			await _asyncReaderWriterLock.AcquireReaderLock(token).ConfigureAwait(false);

			try
			{
				var storage = await StorageTask.Value.ConfigureAwait(false);
				return new DataModelValueSerializer(storage).Load(variable);
			}
			finally
			{
				_asyncReaderWriterLock.ReleaseReaderLock();
			}
		}

		public async ValueTask SetValue(string variable, DataModelValue value, CancellationToken token)
		{
			await _asyncReaderWriterLock.AcquireWriterLock(token).ConfigureAwait(false);

			try
			{
				var storage = await StorageTask.Value.ConfigureAwait(false);
				new DataModelValueSerializer(storage).Save(variable, value);

				await storage.CheckPoint(level: 0, token).ConfigureAwait(false);
			}
			finally
			{
				_asyncReaderWriterLock.ReleaseWriterLock();
			}
		}

		[SuppressMessage(category: "Performance", checkId: "CA1822:Mark members as static", Justification = "<Pending>")]
		public string CreateValue(string lastValue, string? rule, string? template)
		{
			var query = EnumeratePredefinedValues(template, rule);

			if (lastValue.Length > 0)
			{
				query = query.SkipWhile(v => v != lastValue).Skip(1);
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
					_ => Array.Empty<string>()
			};
		}

		private static bool TryGenerate(string? template, string? rule, string value, int index, bool random, out string result)
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