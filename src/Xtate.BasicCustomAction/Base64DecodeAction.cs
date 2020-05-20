using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
#if NETSTANDARD2_1
using System.Buffers;

#endif

namespace Xtate
{
	public class Base64DecodeAction : CustomActionBase
	{
		private const string Content     = "content";
		private const string ContentExpr = "contentexpr";
		private const string Result      = "result";

		public Base64DecodeAction(XmlReader xmlReader, ICustomActionContext access) : base(access)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			RegisterArgument(Content, xmlReader.GetAttribute(ContentExpr), xmlReader.GetAttribute(Content));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add(Content);
			xmlNameTable.Add(ContentExpr);
			xmlNameTable.Add(Result);
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var content = arguments[Content];
			if (content.IsUndefinedOrNull())
			{
				return content;
			}

#if NETSTANDARD2_1
			return OptimizedDecode(content.AsString());

			static string OptimizedDecode(string str)
			{
				var bytes = ArrayPool<byte>.Shared.Rent(str.Length);
				try
				{
					if (!Convert.TryFromBase64String(str, bytes, out var length))
					{
						throw new FormatException("Can't parse Base64 string");
					}

					return Encoding.UTF8.GetString(bytes.AsSpan(start: 0, length));
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(bytes);
				}
			}
#else
			return Encoding.UTF8.GetString(Convert.FromBase64String(content.AsString()));
#endif
		}
	}
}