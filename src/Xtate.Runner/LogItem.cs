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

#nullable enable
namespace Xtate.Runner
{
	public class LogItem
	{
		private readonly string _message;

		public LogItem(string message,
					   DataModelObject? dataModel,
					   string? dataModelAsText,
					   string? dataAsText,
					   Exception? exception)
		{
			_message = message;
			DataModelAsText = dataModelAsText;
			DataAsText = dataAsText;
			DataModel = dataModel;
			Exception = exception;
		}

		public string? DataModelAsText { get; }

		public string? DataAsText { get; }

		public DataModelObject? DataModel { get; }

		public Exception? Exception { get; }

		public override string ToString() => _message;
	}
}