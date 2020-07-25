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

		private readonly string _op;

		public OperationAction(XmlReader xmlReader, ICustomActionContext access) : base(access)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			_op = xmlReader.GetAttribute(Operation);

			RegisterArgument(Left, xmlReader.GetAttribute(LeftExpr), xmlReader.GetAttribute(Left));
			RegisterArgument(Right, xmlReader.GetAttribute(RightExpr), xmlReader.GetAttribute(Right));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			return _op switch
			{
					"emailMatch" => new DataModelValue(EmailMatch(arguments[Left].AsStringOrDefault(), arguments[Right])),
					_ => Infrastructure.UnexpectedValue<DataModelValue>()
			};
		}

		private static bool EmailMatch(string? email, string? pattern)
		{
			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pattern))
			{
				return false;
			}

			pattern = Regex.Escape(pattern).Replace(oldValue: "\\*", newValue: ".*");

			return Regex.Match(email, pattern, RegexOptions.IgnoreCase).Success;
		}

		private static bool EmailMatch(string? email, DataModelValue value)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				return false;
			}

			if (value.Type == DataModelValueType.String)
			{
				return EmailMatch(email, value.AsString());
			}

			if (value.Type == DataModelValueType.Array)
			{
				return value.AsArrayOrEmpty().Any(i => EmailMatch(email, i.AsStringOrDefault()));
			}

			return false;
		}
	}
}