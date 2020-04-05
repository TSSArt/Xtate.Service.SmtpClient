using System.Collections.Immutable;

namespace TSSArt.StateMachine.Test
{
	public static class IoProcessorOptionsTestBuilder
	{
		public delegate void IoProcessorOptionsSetup(ref IoProcessorOptions options);

		public static IoProcessorOptions Create(IoProcessorOptionsSetup build)
		{
			var options = new IoProcessorOptions
						  {
								  DataModelHandlerFactories = ImmutableArray<IDataModelHandlerFactory>.Empty,
								  EventProcessorFactories = ImmutableArray<IEventProcessorFactory>.Empty,
								  ServiceFactories = ImmutableArray<IServiceFactory>.Empty
						  };

			build(ref options);

			return options;
		}
	}
}