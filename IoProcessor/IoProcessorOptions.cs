using System;
using System.Collections.Generic;
using System.Threading;

namespace TSSArt.StateMachine
{
	public class IoProcessorOptions
	{
		public ICollection<IEventProcessor>          EventProcessors           { get; } = new List<IEventProcessor>();
		public ICollection<IServiceFactory>          ServiceFactories          { get; } = new List<IServiceFactory>();
		public ICollection<IDataModelHandlerFactory> DataModelHandlerFactories { get; } = new List<IDataModelHandlerFactory>();
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