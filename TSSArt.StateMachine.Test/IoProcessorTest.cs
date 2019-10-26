using System;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TSSArt.StateMachine.EcmaScript;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class IoProcessorTest
	{
		[TestMethod]
		public async Task SimpleTest()
		{
			var resourceLoaderMock = new Mock<IResourceLoader>();

			var task = new ValueTask<Resource>(new Resource(new Uri("http://none"), new ContentType(), content: "content"));
			resourceLoaderMock.Setup(e => e.Request(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(task);

			var options = new IoProcessorOptions
						  {
								  StateMachineProvider = new StateMachineProvider(),
								  ResourceLoader = resourceLoaderMock.Object
						  };
			options.DataModelHandlerFactories.Add(EcmaScriptDataModelHandler.Factory);

			var ioProcessor = new IoProcessor(options);
			await ioProcessor.Start(new Uri(@"D:\Ser\Projects\T.S.S.Art\MID\PoC\TSSArt.StateMachine.Test\Resources\All.xml"));
		}
	}

	public class StateMachineProvider : IStateMachineProvider
	{
		public IStateMachine GetStateMachine(Uri source)
		{
			using var stream = new FileStream(source.AbsolutePath, FileMode.Open);
			var xmlReader = XmlReader.Create(stream);

			var director = new ScxmlDirector(xmlReader, new BuilderFactory());

			return director.ConstructStateMachine();
		}
	}
}