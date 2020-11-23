#region Copyright © 2019-2020 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using MimeKit;

#if !NET461 && !NETSTANDARD2_0
using System.Buffers;

#endif

namespace Xtate.CustomAction
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

		protected override void Initialize(XmlReader xmlReader)
		{
			if (xmlReader is null) throw new ArgumentNullException(nameof(xmlReader));

			RegisterArgument(Content, ExpectedValueType.String, xmlReader.GetAttribute(ContentExpr), xmlReader.GetAttribute(Content));
			RegisterArgument(Xpath, ExpectedValueType.String, xmlReader.GetAttribute(XpathExpr), xmlReader.GetAttribute(Xpath));
			RegisterArgument(Attr, ExpectedValueType.String, xmlReader.GetAttribute(AttrExpr), xmlReader.GetAttribute(Attr));
			RegisterArgument(Regex, ExpectedValueType.String, xmlReader.GetAttribute(RegexExpr), xmlReader.GetAttribute(Regex));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> args)
		{
			if (args is null) throw new ArgumentNullException(nameof(args));

			var source = args[Content].AsStringOrDefault();
			var xpath = args[Xpath].AsStringOrDefault();
			var attr = args[Attr].AsStringOrDefault();
			var pattern = args[Regex].AsStringOrDefault();

			return source is not null ? Parse(source, xpath, attr, pattern) : DataModelValue.Null;
		}

		private static DataModelValue Parse(string content, string? xpath, string? attr, string? pattern)
		{
			MimeMessage message;

#if NET461 || NETSTANDARD2_0
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
			{
				message = MimeMessage.Load(stream);
			}
#else
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
#endif

			var text = message.TextBody;
			var html = message.HtmlBody;
			if (html is not null)
			{
				var htmlDocument = new HtmlDocument();

				htmlDocument.Load(html);

				return CaptureEntry(htmlDocument, xpath, attr, pattern);
			}

			if (text is null)
			{
				return default;
			}

			if (pattern is null)
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

			var list = new DataModelList();
			foreach (var name in groupNames)
			{
				list.Add(name, match.Groups[name].Value);
			}

			return list;
		}

		private static DataModelValue CaptureEntry(HtmlDocument htmlDocument, string? xpath, string? attr, string? pattern)
		{
			var nodes = xpath is not null ? htmlDocument.DocumentNode.SelectNodes(xpath) : Enumerable.Repeat(htmlDocument.DocumentNode, count: 1);

			foreach (var node in nodes)
			{
				var text = attr is not null ? node.GetAttributeValue(attr, def: null) : node.InnerHtml;

				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}

				if (pattern is null)
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

				var list = new DataModelList();
				foreach (var name in groupNames)
				{
					list.Add(name, match.Groups[name].Value);
				}

				return list;
			}

			return default;
		}
	}
}