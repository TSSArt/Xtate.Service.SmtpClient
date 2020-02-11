using System.Collections.Immutable;

namespace TSSArt.StateMachine.Test
{
	public static class IoProcessorOptionsBuilder
	{
		public delegate void IoProcessorOptionsSetup(ref IoProcessorOptions options);

		public static IoProcessorOptions Create(IoProcessorOptionsSetup build)
		{
			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = ImmutableArray<IDataModelHandlerFactory>.Empty,
								  EventProcessors = ImmutableArray<IEventProcessor>.Empty,
								  ServiceFactories = ImmutableArray<IServiceFactory>.Empty
						  };

			build(ref options);

			return options;
		}
	}
}