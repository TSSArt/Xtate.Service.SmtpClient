using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IResourceLoader
	{
		ValueTask<Resource>  Request(Uri uri, CancellationToken token);
		ValueTask<XmlReader> RequestXmlReader(Uri uri, XmlReaderSettings? readerSettings = null, XmlParserContext? parserContext = null, CancellationToken token = default);
	}
}