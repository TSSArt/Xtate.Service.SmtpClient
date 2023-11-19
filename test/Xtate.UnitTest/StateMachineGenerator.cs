#region Copyright © 2019-2021 Sergii Artemenko

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

using System.IO;
using System.Xml;
using Xtate.Builder;
using Xtate.Core;
using Xtate.IoC;
using Xtate.Scxml;

namespace Xtate.Test
{
	public static class StateMachineGenerator
	{
		private static IStateMachine FromScxml(string scxml)
		{
			var services = new ServiceCollection();
			services.RegisterEcmaScriptDataModelHandler();
			services.RegisterStateMachineFactory();
			services.AddForwarding<IScxmlStateMachine>(_=> new ScxmlStateMachine(scxml));
			var serviceProvider = services.BuildProvider();
			return serviceProvider.GetRequiredService<IStateMachine>().Result;

			using var stringReader = new StringReader(scxml);
			XmlNameTable nt = new NameTable();
			var xmlNamespaceManager = new XmlNamespaceManager(nt);
			using var xmlReader = XmlReader.Create(stringReader, settings: null, new XmlParserContext(nt, xmlNamespaceManager, xmlLang: default, xmlSpace: default));

			var serviceLocator = ServiceLocator.Create(collection => { } /*s => s.AddForwarding<IStateMachineValidator, StateMachineValidator>()*/);
			var scxmlDirector = serviceLocator.GetService<ScxmlDirector, XmlReader>(xmlReader);
			//var scxmlDirector = new ScxmlDirector(xmlReader, serviceLocator.GetService<IBuilderFactory>(), new ScxmlDirectorOptions(serviceLocator) { NamespaceResolver = xmlNamespaceManager });
			return scxmlDirector.ConstructStateMachine().SynchronousGetResult();
		}

		public static IStateMachine FromInnerScxml_EcmaScript(string innerScxml) =>
			FromScxml("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + innerScxml + "</scxml>");
	}
}