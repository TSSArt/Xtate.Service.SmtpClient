using System.IO;
using System.Xml;

namespace Xtate.Test
{
	public static class StateMachineGenerator
	{
		private static IStateMachine FromScxml(string scxml)
		{
			using var stringReader = new StringReader(scxml);
			XmlNameTable nt = new NameTable();
			var xmlNamespaceManager = new XmlNamespaceManager(nt);
			using var xmlReader = XmlReader.Create(stringReader, settings: null, new XmlParserContext(nt, xmlNamespaceManager, xmlLang: default, xmlSpace: default));

			return new ScxmlDirector(xmlReader, BuilderFactory.Instance, DefaultErrorProcessor.Instance, xmlNamespaceManager).ConstructStateMachine(StateMachineValidator.Instance);
		}

		public static IStateMachine FromInnerScxml_EcmaScript(string innerScxml) =>
				FromScxml("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + innerScxml + "</scxml>");
	}
}