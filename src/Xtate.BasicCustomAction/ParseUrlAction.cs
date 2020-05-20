using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.AspNetCore.WebUtilities;

namespace Xtate
{
	public class ParseUrlAction : CustomActionBase
	{
		private const string Url           = "url";
		private const string UrlExpr       = "urlexpr";
		private const string Parameter     = "parameter";
		private const string ParameterExpr = "parameterexpr";
		private const string Result        = "result";

		public ParseUrlAction(XmlReader xmlReader, ICustomActionContext access) : base(access)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			RegisterArgument(Url, xmlReader.GetAttribute(UrlExpr), xmlReader.GetAttribute(Url));
			RegisterArgument(Parameter, xmlReader.GetAttribute(ParameterExpr), xmlReader.GetAttribute(Parameter));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add(Url);
			xmlNameTable.Add(UrlExpr);
			xmlNameTable.Add(Parameter);
			xmlNameTable.Add(ParameterExpr);
			xmlNameTable.Add(Result);
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var uri = new Uri(arguments[Url].AsString(), UriKind.RelativeOrAbsolute);
			var parameter = arguments[Parameter].AsStringOrDefault();
			var parameters = QueryHelpers.ParseNullableQuery(uri.OriginalString);

			if (parameter == null)
			{
				return parameters.ToDataModelObject(p => p.Key, p => p.Value.ToString());
			}

			var values = parameters[parameter];

			return values.Count > 0 ? values[0] : DataModelValue.Null;
		}
	}
}