using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class OperationAction : CustomActionBase
	{
		private readonly string _left;
		private readonly string _op;
		private readonly string _result;
		private readonly string _right;

		public OperationAction(XmlReader xmlReader)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			_left = xmlReader.GetAttribute("left");
			_right = xmlReader.GetAttribute("right");
			_op = xmlReader.GetAttribute("op");
			_result = xmlReader.GetAttribute("result");
		}

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add("left");
			xmlNameTable.Add("right");
			xmlNameTable.Add("op");
			xmlNameTable.Add("result");
		}

		public override ValueTask Execute(IExecutionContext context, CancellationToken token)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			context.DataModel[_result] = _op switch
			{
					"emailMatch" => new DataModelValue(EmailMatch(context.DataModel[_left].AsStringOrDefault(), context.DataModel[_right])),
					_ => Infrastructure.UnexpectedValue<DataModelValue>()
			};

			return default;
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