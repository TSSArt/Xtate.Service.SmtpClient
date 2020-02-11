using System;
using System.Collections.Immutable;
using System.Threading;

namespace TSSArt.StateMachine
{
	public struct IoProcessorOptions
	{
		public ImmutableArray<IEventProcessor>          EventProcessors            { get; set; }
		public ImmutableArray<IServiceFactory>          ServiceFactories           { get; set; }
		public ImmutableArray<IDataModelHandlerFactory> DataModelHandlerFactories  { get; set; }
		public ImmutableArray<ICustomActionProvider>    CustomActionProviders      { get; set; }
		public ImmutableDictionary<string, string>      Configuration              { get; set; }
		public bool                                     SynchronousEventProcessing { get; set; }
		public IStateMachineProvider                    StateMachineProvider       { get; set; }
		public ILogger                                  Logger                     { get; set; }
		public PersistenceLevel                         PersistenceLevel           { get; set; }
		public IStorageProvider                         StorageProvider            { get; set; }
		public IResourceLoader                          ResourceLoader             { get; set; }
		public TimeSpan                                 SuspendIdlePeriod          { get; set; }
		public CancellationToken                        SuspendToken               { get; set; }
		public CancellationToken                        StopToken                  { get; set; }
	}
}