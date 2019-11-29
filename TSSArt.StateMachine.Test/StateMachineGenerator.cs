using System.IO;
using System.Xml;

namespace TSSArt.StateMachine.Test
{
	public static class StateMachineGenerator
	{
		public static IStateMachine FromScxml(string scxml)
		{
			using var stringReader = new StringReader(scxml);
			using var xmlReader = XmlReader.Create(stringReader);
			return new ScxmlDirector(xmlReader, new BuilderFactory()).ConstructStateMachine();
		}

		public static IStateMachine FromInnerScxml_EcmaScript(string innerScxml) =>
				FromScxml("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'>" + innerScxml + "</scxml>");
	}
}