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

namespace Xtate.CustomAction
{
	public class BasicCustomActionFactory : CustomActionFactoryBase
	{
		public static ICustomActionFactory Instance { get; } = new BasicCustomActionFactory();

		protected override void Register(ICustomActionCatalog catalog)
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));

			const string ns = "http://xtate.net/scxml/customaction/basic";

			catalog.Register(ns, name: "base64decode", () => new Base64DecodeAction());
			catalog.Register(ns, name: "parseUrl", () => new ParseUrlAction());
			catalog.Register(ns, name: "format", () => new FormatAction());
			catalog.Register(ns, name: "operation", () => new OperationAction());
		}
	}
}