using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using MimeKit;

#if NETSTANDARD2_1
using System.Buffers;

#endif

namespace TSSArt.StateMachine.Services
{
	public class ParseEmail : CustomActionBase
	{
		private const string Content     = "content";
		private const string ContentExpr = "contentexpr";
		private const string Xpath       = "xpath";
		private const string XpathExpr   = "xpathexpr";
		private const string Attr        = "attr";
		private const string AttrExpr    = "attrexpr";
		private const string Regex       = "regex";
		private const string RegexExpr   = "regexexpr";
		private const string Result      = "result";

		public ParseEmail(XmlReader xmlReader, ICustomActionContext access) : base(access)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			RegisterArgument(Content, xmlReader.GetAttribute(ContentExpr), xmlReader.GetAttribute(Content));
			RegisterArgument(Xpath, xmlReader.GetAttribute(XpathExpr), xmlReader.GetAttribute(Xpath));
			RegisterArgument(Attr, xmlReader.GetAttribute(AttrExpr), xmlReader.GetAttribute(Attr));
			RegisterArgument(Regex, xmlReader.GetAttribute(RegexExpr), xmlReader.GetAttribute(Regex));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add(Content);
			xmlNameTable.Add(ContentExpr);
			xmlNameTable.Add(Xpath);
			xmlNameTable.Add(XpathExpr);
			xmlNameTable.Add(Attr);
			xmlNameTable.Add(AttrExpr);
			xmlNameTable.Add(Regex);
			xmlNameTable.Add(RegexExpr);
			xmlNameTable.Add(Result);
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> args)
		{
			if (args == null) throw new ArgumentNullException(nameof(args));

			var source = args[Content].AsStringOrDefault();
			var xpath = args[Xpath].AsStringOrDefault();
			var attr = args[Attr].AsStringOrDefault();
			var pattern = args[Regex].AsStringOrDefault();

			return source != null ? Parse(source, xpath, attr, pattern) : DataModelValue.Null;
		}

		private static DataModelValue Parse(string content, string? xpath, string? attr, string? pattern)
		{
			MimeMessage message;

#if NETSTANDARD2_1
			var bytes = ArrayPool<byte>.Shared.Rent(Encoding.ASCII.GetMaxByteCount(content.Length));
			try
			{
				var length = Encoding.ASCII.GetBytes(content, bytes);
				using var stream = new MemoryStream(bytes, index: 0, length);
				message = MimeMessage.Load(stream);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(bytes);
			}
#else
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
			{
				message = MimeMessage.Load(stream);
			}
#endif

			var text = message.TextBody;
			var html = message.HtmlBody;
			if (html != null)
			{
				var htmlDocument = new HtmlDocument();

				htmlDocument.Load(html);

				return CaptureEntry(htmlDocument, xpath, attr, pattern);
			}

			if (text == null)
			{
				return default;
			}

			if (pattern == null)
			{
				return text;
			}

			var regex = new Regex(pattern);
			var match = regex.Match(text);

			if (!match.Success)
			{
				return default;
			}

			if (match.Groups.Count == 1)
			{
				return match.Groups[0].Value;
			}

			var groupNames = regex.GetGroupNames();

			var obj = new DataModelObject(groupNames.Length);
			foreach (var name in groupNames)
			{
				obj[name] = match.Groups[name].Value;
			}

			return obj;
		}

		private static DataModelValue CaptureEntry(HtmlDocument htmlDocument, string? xpath, string? attr, string? pattern)
		{
			var nodes = xpath != null ? htmlDocument.DocumentNode.SelectNodes(xpath) : Enumerable.Repeat(htmlDocument.DocumentNode, count: 1);

			foreach (var node in nodes)
			{
				var text = attr != null ? node.GetAttributeValue(attr, def: null) : node.InnerHtml;

				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}

				if (pattern == null)
				{
					return text;
				}

				var regex = new Regex(pattern);
				var match = regex.Match(text);

				if (!match.Success)
				{
					continue;
				}

				if (match.Groups.Count == 1)
				{
					return match.Groups[0].Value;
				}

				var groupNames = regex.GetGroupNames();

				var obj = new DataModelObject(groupNames.Length);
				foreach (var name in groupNames)
				{
					obj[name] = match.Groups[name].Value;
				}

				return obj;
			}

			return default;
		}
	}
}