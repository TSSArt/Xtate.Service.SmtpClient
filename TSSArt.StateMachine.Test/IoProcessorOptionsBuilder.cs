using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine.Test
{
	public static class IoProcessorOptionsBuilder
	{
		public static IoProcessorOptions Create(Action<IoProcessorOptions> build)
		{
			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = new List<IDataModelHandlerFactory>(),
								  EventProcessors = new List<IEventProcessor>(),
								  ServiceFactories = new List<IServiceFactory>()
						  };

			build(options);

			return options;
		}
	}
}