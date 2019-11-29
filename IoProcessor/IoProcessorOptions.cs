using System;
using System.Collections.Generic;
using System.Threading;

namespace TSSArt.StateMachine
{
	public struct IoProcessorOptions
	{
		public ICollection<IEventProcessor>          EventProcessors           { get; set; }
		public ICollection<IServiceFactory>          ServiceFactories          { get; set; }
		public ICollection<IDataModelHandlerFactory> DataModelHandlerFactories { get; set; }
		public ICollection<ICustomActionProvider>    CustomActionProviders     { get; set; }
		public IStateMachineProvider                 StateMachineProvider      { get; set; }
		public ILogger                               Logger                    { get; set; }
		public PersistenceLevel                      PersistenceLevel          { get; set; }
		public IStorageProvider                      StorageProvider           { get; set; }
		public IResourceLoader                       ResourceLoader            { get; set; }
		public TimeSpan                              SuspendIdlePeriod         { get; set; }
		public CancellationToken                     SuspendToken              { get; set; }
		public CancellationToken                     StopToken                 { get; set; }
	}
}