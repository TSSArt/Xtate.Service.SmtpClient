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
using Serilog.Core;
using Serilog.Events;

namespace Xtate
{
	public class DataModelListDestructuringPolicy : IDestructuringPolicy
	{
	#region Interface IDestructuringPolicy

		public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
		{
			if (!(value is DataModelList list))
			{
				result = default;

				return false;
			}

			result = GetLogEventPropertyValue(list);

			return true;
		}

	#endregion

		private static LogEventPropertyValue GetLogEventPropertyValue(in DataModelValue value) =>
				value.Type switch
				{
						DataModelValueType.Undefined => new ScalarValue(value.ToObject()),
						DataModelValueType.Null => new ScalarValue(value.ToObject()),
						DataModelValueType.String => new ScalarValue(value.ToObject()),
						DataModelValueType.Number => new ScalarValue(value.ToObject()),
						DataModelValueType.DateTime => new ScalarValue(value.ToObject()),
						DataModelValueType.Boolean => new ScalarValue(value.ToObject()),
						DataModelValueType.Array => GetLogEventPropertyValue(value.AsList()),
						DataModelValueType.Object => GetLogEventPropertyValue(value.AsList()),
						_ => Infrastructure.UnexpectedValue<LogEventPropertyValue>()
				};

		private static LogEventPropertyValue GetLogEventPropertyValue(DataModelList list)
		{
			var index = 0;
			foreach (var entry in list.Entries)
			{
				if (index ++ != entry.Index)
				{
					return new StructureValue(EnumerateEntries(true));
				}
			}

			if (list.GetMetadata() is { })
			{
				return new StructureValue(EnumerateEntries(false));
			}

			foreach (var entry in list.Entries)
			{
				if (entry.Key is { } || entry.Metadata is { })
				{
					return new StructureValue(EnumerateEntries(false));
				}
			}

			if (list.Count == 0 && list is DataModelObject)
			{
				return new StructureValue(Array.Empty<LogEventProperty>());
			}

			return new SequenceValue(EnumerateValues());

			IEnumerable<LogEventProperty> EnumerateEntries(bool showIndex)
			{
				foreach (var entry in list.Entries)
				{
					var name = GetName(entry.Key);
					yield return new LogEventProperty(name, GetLogEventPropertyValue(entry.Value));

					if (showIndex)
					{
						yield return new LogEventProperty(name + @":(index)", new ScalarValue(entry.Index));
					}

					if (entry.Metadata is { } entryMetadata)
					{
						yield return new LogEventProperty(name + @":(meta)", GetLogEventPropertyValue(entryMetadata));
					}
				}

				if (list.GetMetadata() is { } metadata)
				{
					yield return new LogEventProperty(name: @"(meta)", GetLogEventPropertyValue(metadata));
				}
			}

			IEnumerable<LogEventPropertyValue> EnumerateValues()
			{
				foreach (var value in list.Values)
				{
					yield return GetLogEventPropertyValue(value);
				}
			}
		}

		private static string GetName(string? key)
		{
			if (key is null)
			{
				return "(null)";
			}

			if (string.IsNullOrWhiteSpace(key))
			{
				return "(" + key + ")";
			}

			return key;
		}
	}
}