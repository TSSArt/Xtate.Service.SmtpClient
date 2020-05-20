using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace Xtate
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

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add(Template);
			xmlNameTable.Add(TemplateExpr);
			xmlNameTable.Add(Result);
			xmlNameTable.Add(ArgsExpr);
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