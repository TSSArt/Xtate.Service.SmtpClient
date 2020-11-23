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
using System.Text;
using System.Xml;

#if !NET461 && !NETSTANDARD2_0
using System.Buffers;

#endif

namespace Xtate.CustomAction
{
	public class Base64DecodeAction : CustomActionBase
	{
		private const string Content     = "content";
		private const string ContentExpr = "contentexpr";
		private const string Result      = "result";

		protected override void Initialize(XmlReader xmlReader)
		{
			if (xmlReader is null) throw new ArgumentNullException(nameof(xmlReader));

			RegisterArgument(Content, ExpectedValueType.String, xmlReader.GetAttribute(ContentExpr), xmlReader.GetAttribute(Content));
			RegisterResultLocation(xmlReader.GetAttribute(Result));
		}

		protected override DataModelValue Evaluate(IReadOnlyDictionary<string, DataModelValue> arguments)
		{
			if (arguments is null) throw new ArgumentNullException(nameof(arguments));

			var content = arguments[Content];
			if (content.IsUndefinedOrNull())
			{
				return content;
			}

#if NET461 || NETSTANDARD2_0
			return Encoding.UTF8.GetString(Convert.FromBase64String(content.AsString()));
#else
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
#endif
		}
	}
}