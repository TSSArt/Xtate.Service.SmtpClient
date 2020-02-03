using System;
using System.Collections./**/Immutable;
using System.Threading;

namespace TSSArt.StateMachine
{
	public struct IoProcessorOptions
	{
		public IReadOnlyCollection<IEventProcessor>          EventProcessors            { get; set; }
		public IReadOnlyCollection<IServiceFactory>          ServiceFactories           { get; set; }
		public IReadOnlyCollection<IDataModelHandlerFactory> DataModelHandlerFactories  { get; set; }
		public IReadOnlyCollection<ICustomActionProvider>    CustomActionProviders      { get; set; }
		public IReadOnlyDictionary<string, string>           Configuration              { get; set; }
		public bool                                          SynchronousEventProcessing { get; set; }
		public IStateMachineProvider                         StateMachineProvider       { get; set; }
		public ILogger                                       Logger                     { get; set; }
		public PersistenceLevel                              PersistenceLevel           { get; set; }
		public IStorageProvider                              StorageProvider            { get; set; }
		public IResourceLoader                               ResourceLoader             { get; set; }
		public TimeSpan                                      SuspendIdlePeriod          { get; set; }
		public CancellationToken                             SuspendToken               { get; set; }
		public CancellationToken                             StopToken                  { get; set; }
	}
}