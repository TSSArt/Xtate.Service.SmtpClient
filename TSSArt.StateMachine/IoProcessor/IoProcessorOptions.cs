using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class IoProcessorOptions
	{
		public ImmutableArray<IEventProcessorFactory>   EventProcessorFactories   { get; set; }
		public ImmutableArray<IServiceFactory>          ServiceFactories          { get; set; }
		public ImmutableArray<IDataModelHandlerFactory> DataModelHandlerFactories { get; set; }
		public ImmutableArray<ICustomActionFactory>     CustomActionFactories     { get; set; }
		public ImmutableDictionary<string, string>      Configuration             { get; set; }
		public ILogger                                  Logger                    { get; set; }
		public PersistenceLevel                         PersistenceLevel          { get; set; }
		public IStorageProvider                         StorageProvider           { get; set; }
		public IResourceLoader                          ResourceLoader            { get; set; }
		public TimeSpan                                 SuspendIdlePeriod         { get; set; }
	}
}