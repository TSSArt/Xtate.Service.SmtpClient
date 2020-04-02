using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class FormatAction : CustomActionBase
	{
		private static readonly Regex RegexReplacer = new Regex(pattern: @"\{\#(\w+)\#\}", RegexOptions.Compiled);

		private readonly string _arguments;
		private readonly string _destination;
		private readonly string _template;

		public FormatAction(XmlReader xmlReader)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			_template = xmlReader.GetAttribute("template");
			_destination = xmlReader.GetAttribute("destination");
			_arguments = xmlReader.GetAttribute("arguments");
		}

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add("template");
			xmlNameTable.Add("destination");
			xmlNameTable.Add("arguments");
		}

		public override ValueTask Execute(IExecutionContext context, CancellationToken token)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			var source = context.DataModel[_template].AsString();
			var arguments = context.DataModel[_arguments].AsObjectOrEmpty();

			var result = RegexReplacer.Replace(source, m => arguments[m.Groups[1].Value].AsStringOrDefault());

			context.DataModel[_destination] = new DataModelValue(result);

			return default;
		}
	}
}