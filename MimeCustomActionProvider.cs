using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using MimeKit;

namespace TSSArt.StateMachine.Services
{
	public class ParseEmail : CustomActionBase
	{
		private readonly string _source;
		private readonly string _destination;
		private readonly string _xpath;
		private readonly string _attr;
		private readonly string _pattern;

		public ParseEmail(XmlReader xmlReader)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			_source = xmlReader.GetAttribute("source");
			_destination = xmlReader.GetAttribute("destination");
			_xpath = xmlReader.GetAttribute("xpath");
			_attr = xmlReader.GetAttribute("attr");
			_pattern = xmlReader.GetAttribute("regex");
		}

		public override ValueTask Action(IExecutionContext context, CancellationToken token)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			var content = context.DataModel[_source].AsString();

			context.DataModel[_destination] = Parse(content);

			return default;
		}

		private DataModelValue Parse(string content)
		{
			var message = MimeMessage.Load(content);

			var text = message.TextBody;
			var html = message.HtmlBody;
			if (html != null)
			{
				var htmlDocument = new HtmlDocument();

				htmlDocument.Load(html);

				var node = _xpath != null ? htmlDocument.DocumentNode.SelectSingleNode(_xpath) : htmlDocument.DocumentNode;

				if (node == null)
				{
					return DataModelValue.Undefined();
				}

				text = _attr != null ? node.GetAttributeValue(_attr, def: null) : node.InnerHtml;
			}

			if (text == null)
			{
				return DataModelValue.Undefined();
			}

			if (_pattern == null)
			{
				return new DataModelValue(text);
			}

			var regex = new Regex(_pattern);
			var match = regex.Match(text);

			if (!match.Success)
			{
				return DataModelValue.Undefined();
			}

			if (match.Groups.Count == 1)
			{
				return new DataModelValue(match.Groups[0].Value);
			}

			var obj = new DataModelObject();
			foreach (string name in regex.GetGroupNames())
			{
				obj[name] = new DataModelValue(match.Groups[name].Value);
			}

			return new DataModelValue(obj);
		}
	}

	[CustomActionProvider("http://tssart.com/scxml/customaction/mime")]
	public class MimeCustomActionProvider : CustomActionProviderBase
	{
		public static readonly ICustomActionProvider Instance = new MimeCustomActionProvider();

		private MimeCustomActionProvider()
		{
			Register(name: "parseEmail", xmlReader => new ParseEmail(xmlReader));
		}
	}
}