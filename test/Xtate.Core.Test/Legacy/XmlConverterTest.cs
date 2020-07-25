using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xtate.Core.Test.Legacy
{
	[TestClass]
	public class XmlConverterTest
	{
		[TestMethod]
		public void ToXmlTest()
		{
			var writer = new StringWriter();
			var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
													 {
															 Indent = true
													 });

			xmlWriter.WriteStartElement("v");

			//	xmlWriter.WriteAttributeString(prefix: @"xmlns", localName: @"xs", ns: null, "http://www.w3.org/2001/XMLSchema");
			//	xmlWriter.WriteAttributeString(prefix: @"xmlns", localName: @"xsi", ns: null, "http://www.w3.org/2001/XMLSchema-instance");
			xmlWriter.WriteStartElement("inv");

			//XmlConverter.WriteValue(xmlWriter, new DataModelValue(5.5));
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();
			xmlWriter.Flush();

			//xmlWriter.

			var _ = writer.ToString();
		}
	}
}