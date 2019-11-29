using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using MimeKit;

namespace TSSArt.StateMachine.Services
{
	public class MimeCustomActionProvider : ICustomActionProvider
	{
		public static readonly ICustomActionProvider Instance = new MimeCustomActionProvider();

		private MimeCustomActionProvider() { }

		public bool CanHandle(string ns, string name) => ns == "http://tssart.com/scxml/customaction/mime" && name == "parseEmail";

		public Func<IExecutionContext, CancellationToken, ValueTask> GetAction(string xml)
		{
			using var stringReader = new StringReader(xml);
			using var xmlReader = XmlReader.Create(stringReader);

			xmlReader.MoveToContent();

			var source = xmlReader.GetAttribute("source");
			var destination = xmlReader.GetAttribute("destination");
			var xpath = xmlReader.GetAttribute("xpath");
			var attr = xmlReader.GetAttribute("attr");
			var pattern = xmlReader.GetAttribute("regex");

			return (context, token) =>
				   {
					   var content = context.DataModel[source].AsString();
					   context.DataModel[destination] = ParseEmail(content);

					   return default;
				   };

			DataModelValue ParseEmail(string content)
			{
				var message = MimeMessage.Load(content);

				var text = message.TextBody;
				var html = message.HtmlBody;
				if (html != null)
				{
					var htmlDocument = new HtmlDocument();

					htmlDocument.Load(html);

					var node = xpath != null ? htmlDocument.DocumentNode.SelectSingleNode(xpath) : htmlDocument.DocumentNode;

					if (node == null)
					{
						return DataModelValue.Undefined();
					}

					text = attr != null ? node.GetAttributeValue(attr, def: null) : node.InnerHtml;
				}

				if (text == null)
				{
					return DataModelValue.Undefined();
				}

				if (pattern == null)
				{
					return new DataModelValue(text);
				}

				var regex = new Regex(pattern);
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
	}
}