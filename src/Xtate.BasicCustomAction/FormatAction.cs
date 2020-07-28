#region Copyright © 2019-2020 Sergii Artemenko
// 
// This file is part of the Xtate project. <http://xtate.net>
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
// 
#endregion

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace Xtate.CustomAction
{
	public class FormatAction : CustomActionBase
	{
		private const string Template     = "template";
		private const string TemplateExpr = "templateexpr";
		private const string Result       = "result";
		private const string ArgsExpr     = "argsexpr";

		private static readonly Regex RegexReplacer = new Regex(pattern: @"\{\#(\w+)\#\}", RegexOptions.Compiled);

		public FormatAction(XmlReader xmlReader, ICustomActionContext access) : base(access)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			RegisterArgument(Template, xmlReader.GetAttribute(TemplateExpr), xmlReader.GetAttribute(Template));
			RegisterArgument(ArgsExpr, xmlReader.GetAttribute(ArgsExpr));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var template = arguments[Template].AsString();
			var args = arguments[ArgsExpr].AsObjectOrEmpty();

			return RegexReplacer.Replace(template, m => args[m.Groups[1].Value].AsStringOrDefault());
		}
	}
}