using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class StorageAction : CustomActionBase
	{
		private readonly string  _location;
		private readonly string? _operation;
		private readonly string? _rule;
		private readonly string? _template;

		public StorageAction(XmlReader xmlReader)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			_location = xmlReader.GetAttribute("location");
			_operation = xmlReader.GetAttribute("operation")?.ToUpperInvariant();
			_template = xmlReader.GetAttribute("template")?.ToUpperInvariant();
			_rule = xmlReader.GetAttribute("rule");


			//<storage xmlns="http://tssart.com/scxml/customaction/mid" location="username"
			//operation="create" template="userid" rule="[a-z]{1,20}" />
			//<mid:storage location="username" operation="get" variable="username" />
			//<mid:storage location="password" operation="set" variable="password" />
		}

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add("location");
			xmlNameTable.Add("operation");
			xmlNameTable.Add("template");
			xmlNameTable.Add("rule");
		}

		public override ValueTask Execute(IExecutionContext context, CancellationToken token)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			if (_operation == "CREATE")
			{
				var locationValue = context.DataModel[_location];
				var lastValue = locationValue.AsStringOrDefault() ?? string.Empty;
				var value = CreateValue(lastValue);
				context.DataModel[_location] = new DataModelValue(value);

				return default;
			}

			throw new NotSupportedException($"Unknown operation [{_operation}]");
		}

		private string CreateValue(string lastValue)
		{
			var query = EnumeratePredefinedValues();

			if (lastValue.Length > 0)
			{
				query = query.SkipWhile(v => v != lastValue).Skip(1);
			}

			return query.FirstOrDefault() ?? CreateRandomValue();
		}

		private string CreateRandomValue()
		{
			string result;

			foreach (var value in GetPredefinedValues())
			{
				if (TryGenerate(value, index: 0, random: true, out result))
				{
					return result;
				}
			}

			if (TryGenerate(string.Empty, index: 0, random: true, out result))
			{
				return result;
			}

			throw new InvalidOperationException(@"Can't generate value.");
		}

		private IEnumerable<string> EnumeratePredefinedValues()
		{
			foreach (var value in GetPredefinedValues())
			{
				if (TryGenerate(value, index: 0, random: false, out var result))
				{
					yield return result;
				}
			}

			foreach (var value in GetPredefinedValues())
			{
				for (var i = 1; i < 9; i ++)
				{
					if (TryGenerate(value, i, random: false, out var result))
					{
						yield return result;
					}
				}
			}
		}

		private string[] GetPredefinedValues()
		{
			return _template switch
			{
					"USERID" => new[] { "tadex", "xtadex" },
					_ => Array.Empty<string>()
			};
		}

		private bool TryGenerate(string value, int index, bool random, out string result)
		{
			result = value;

			if (index > 0)
			{
				result += index;
			}

			if (random)
			{
				var length = _template == "PASSWORD" ? 16 : 2;
				for (var i = 0; i < length; i ++)
				{
					result += new Random().Next(minValue: 0, maxValue: 9);
				}
			}

			return Regex.IsMatch(result, _rule);
		}
	}
}