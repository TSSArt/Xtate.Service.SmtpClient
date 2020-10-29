﻿#region Copyright © 2019-2020 Sergii Artemenko

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
using System.Collections;
using System.Collections.Generic;

namespace Xtate
{
	public partial class DataModelList : IList<DataModelValue>
	{
	#region Interface ICollection<DataModelValue>

		public void CopyTo(DataModelValue[] array, int index)
		{
			if (array is null) throw new ArgumentNullException(nameof(array));
			if (index < 0 || index >= array.Length) throw new ArgumentOutOfRangeException(nameof(index), Resources.Exception_Index_should_be_non_negative_and_less_then_aarray_size);
			if (Count - index < array.Length) throw new ArgumentException(Resources.Exception_Destination_array_is_not_long_enough, nameof(array));

			foreach (var value in Values)
			{
				array[index ++] = value;
			}
		}

		public void Add(DataModelValue value) => Add(key: default, value, metadata: default);

		public bool Contains(DataModelValue value) => GetIndex(value) >= 0;

		public bool Remove(DataModelValue item)
		{
			var index = GetIndex(item);

			return index >= 0 && Remove(index);
		}

		bool ICollection<DataModelValue>.IsReadOnly => Access != DataModelAccess.Writable;

	#endregion

	#region Interface IEnumerable

		IEnumerator IEnumerable.GetEnumerator() => new ValueEnumerator(this);

	#endregion

	#region Interface IEnumerable<DataModelValue>

		IEnumerator<DataModelValue> IEnumerable<DataModelValue>.GetEnumerator() => new ValueEnumerator(this);

	#endregion

	#region Interface IList<DataModelValue>

		public DataModelValue this[int index]
		{
			get
			{
				TryGet(index, out var entry);

				return entry.Value;
			}

			set => Set(index, key: default, value, metadata: default);
		}

		public int IndexOf(DataModelValue item) => GetIndex(item);

		public void Insert(int index, DataModelValue item) => Insert(index, key: default, item, metadata: default);

		public void RemoveAt(int index) => Remove(index);

	#endregion

		public ValueEnumerator GetEnumerator() => new ValueEnumerator(this);

		private int GetIndex(in DataModelValue value)
		{
			foreach (var entry in Entries)
			{
				if (entry.Value == value)
				{
					return entry.Index;
				}
			}

			return -1;
		}
	}
}