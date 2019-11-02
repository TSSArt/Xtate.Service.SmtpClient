using System;
using System.Collections.Generic;
using System.Threading;

namespace TSSArt.StateMachine
{
	public struct IoProcessorOptions
	{
		public ICollection<IEventProcessor>          EventProcessors;
		public ICollection<IServiceFactory>          ServiceFactories;
		public ICollection<IDataModelHandlerFactory> DataModelHandlerFactories;
		public IStateMachineProvider                 StateMachineProvider;
		public ILogger                               Logger;
		public PersistenceLevel                      PersistenceLevel;
		public IStorageProvider                      StorageProvider;
		public IResourceLoader                       ResourceLoader;
		public TimeSpan                              SuspendIdlePeriod;
		public CancellationToken                     SuspendToken;
		public CancellationToken                     StopToken;
	}
}