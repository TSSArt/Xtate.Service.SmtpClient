using System.Collections.Immutable;

namespace TSSArt.StateMachine.Test
{
	public static class StateMachineHostOptionsTestBuilder
	{
		public delegate void StateMachineHostOptionsSetup(ref StateMachineHostOptions options);

		public static StateMachineHostOptions Create(StateMachineHostOptionsSetup build)
		{
			var options = new StateMachineHostOptions
						  {
								  DataModelHandlerFactories = ImmutableArray<IDataModelHandlerFactory>.Empty,
								  IoProcessorFactories = ImmutableArray<IIoProcessorFactory>.Empty,
								  ServiceFactories = ImmutableArray<IServiceFactory>.Empty
						  };

			build(ref options);

			return options;
		}
	}
}