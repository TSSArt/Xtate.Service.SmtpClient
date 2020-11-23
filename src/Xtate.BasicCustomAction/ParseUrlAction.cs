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
using System.Xml;
using Microsoft.AspNetCore.WebUtilities;

namespace Xtate.CustomAction
{
	public class ParseUrlAction : CustomActionBase
	{
		private const string Url           = "url";
		private const string UrlExpr       = "urlexpr";
		private const string Parameter     = "parameter";
		private const string ParameterExpr = "parameterexpr";
		private const string Result        = "result";

		protected override void Initialize(XmlReader xmlReader)
		{
			if (xmlReader is null) throw new ArgumentNullException(nameof(xmlReader));

			RegisterArgument(Url, ExpectedValueType.String, xmlReader.GetAttribute(UrlExpr), xmlReader.GetAttribute(Url));
			RegisterArgument(Parameter, ExpectedValueType.String, xmlReader.GetAttribute(ParameterExpr), xmlReader.GetAttribute(Parameter));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments)
		{
			if (arguments is null) throw new ArgumentNullException(nameof(arguments));

			var uri = new Uri(arguments[Url].AsString(), UriKind.RelativeOrAbsolute);
			var parameters = QueryHelpers.ParseNullableQuery(uri.OriginalString);

			if (arguments[Parameter].AsStringOrDefault() is { } parameter)
			{
				var values = parameters[parameter];

				return values.Count > 0 ? values[0] : DataModelValue.Null;
			}

			var result = new DataModelList();

			foreach (var pair in parameters)
			{
				foreach (var value in pair.Value)
				{
					result.Add(pair.Key, value);
				}
			}

			return result;
		}
	}
}