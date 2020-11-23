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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Xtate.CustomAction
{
	public class StorageAction : CustomActionBase
	{
		private const string Location  = "location";
		private const string Operation = "operation";
		private const string Template  = "template";
		private const string Rule      = "rule";
		private const string Variable  = "variable";

		private readonly StorageActionService _storageActionService;

		private string? _operation;
		private string? _rule;
		private string? _template;
		private string? _variable;

		internal StorageAction(StorageActionService storageActionService) => _storageActionService = storageActionService;

		protected override void Initialize(XmlReader xmlReader)
		{
			if (xmlReader is null) throw new ArgumentNullException(nameof(xmlReader));

			_operation = xmlReader.GetAttribute(Operation);
			_template = xmlReader.GetAttribute(Template);
			_rule = xmlReader.GetAttribute(Rule);
			_variable = xmlReader.GetAttribute(Variable);

			RegisterArgument(Location, ExpectedValueType.Any, xmlReader.GetAttribute(Location));

			if (_operation is "create" or "get")
			{
				RegisterResultLocation(xmlReader.GetAttribute(Location));
			}

			//<storage xmlns="http://xtate.net/scxml/customaction/mid" location="username" operation="create" template="userid" rule="[a-z]{1,20}" />
			//<mid:storage location="username" operation="get" variable="username" />
			//<mid:storage location="password" operation="set" variable="password" />
		}

		protected override async ValueTask<DataModelValue> EvaluateAsync(IReadOnlyDictionary<string, DataModelValue> arguments, CancellationToken token)
		{
			if (arguments is null) throw new ArgumentNullException(nameof(arguments));

			if (_operation == "create")
			{
				var locationValue = arguments[Location];
				var lastValue = locationValue.AsStringOrDefault() ?? string.Empty;

				return _storageActionService.CreateValue(lastValue, _rule, _template);
			}

			if (_operation == "get" && _variable is not null)
			{
				return await _storageActionService.GetValue(_variable, token).ConfigureAwait(false);
			}

			if (_operation == "set" && _variable is not null)
			{
				await _storageActionService.SetValue(_variable, arguments[Location], token).ConfigureAwait(false);

				return default;
			}

			throw new NotSupportedException($"Unknown operation [{_operation}]");
		}
	}
}