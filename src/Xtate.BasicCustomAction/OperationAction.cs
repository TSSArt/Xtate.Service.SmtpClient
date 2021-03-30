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
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Xtate.CustomAction
{
	public class OperationAction : CustomActionBase
	{
		private const string Left      = "left";
		private const string LeftExpr  = "leftexpr";
		private const string Right     = "right";
		private const string RightExpr = "rightexpr";
		private const string Operation = "op";
		private const string Result    = "result";

		private string? _op;

		protected override void Initialize(XmlReader xmlReader)
		{
			if (xmlReader is null) throw new ArgumentNullException(nameof(xmlReader));

			_op = xmlReader.GetAttribute(Operation);

			RegisterArgument(Left, ExpectedValueType.Any, xmlReader.GetAttribute(LeftExpr), xmlReader.GetAttribute(Left));
			RegisterArgument(Right, ExpectedValueType.Any, xmlReader.GetAttribute(RightExpr), xmlReader.GetAttribute(Right));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments)
		{
			if (arguments is null) throw new ArgumentNullException(nameof(arguments));

			return _op switch
				   {
					   "emailMatch" => new DataModelValue(EmailMatch(arguments[Left].AsStringOrDefault(), arguments[Right])),
					   _            => Infrastructure.UnexpectedValue<DataModelValue>(_op)
				   };
		}

		private static bool EmailMatch(string? email, string? pattern)
		{
			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pattern))
			{
				return false;
			}

			pattern = Regex.Escape(pattern!).Replace(oldValue: "\\*", newValue: ".*");

			return Regex.Match(email!, pattern, RegexOptions.IgnoreCase).Success;
		}

		private static bool EmailMatch(string? email, in DataModelValue value)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				return false;
			}

			if (value.Type == DataModelValueType.String)
			{
				return EmailMatch(email, value.AsString());
			}

			if (value.Type == DataModelValueType.List)
			{
				return value.AsListOrEmpty().Any(i => EmailMatch(email, i.AsStringOrDefault()));
			}

			return false;
		}
	}
}