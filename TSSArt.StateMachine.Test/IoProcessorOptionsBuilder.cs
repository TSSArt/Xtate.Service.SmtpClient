using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine.Test
{
	public static class IoProcessorOptionsBuilder
	{
		public delegate void IoProcessorOptionsSetup(ref IoProcessorOptions options);

		public static IoProcessorOptions Create(IoProcessorOptionsSetup build)
		{
			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = new List<IDataModelHandlerFactory>(),
								  EventProcessors = new List<IEventProcessor>(),
								  ServiceFactories = new List<IServiceFactory>()
						  };

			build(ref options);

			return options;
		}
	}
}