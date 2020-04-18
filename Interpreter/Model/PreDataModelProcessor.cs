using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class PreDataModelProcessor : StateMachineVisitor
	{
		private readonly ImmutableArray<ICustomActionFactory> _customActionProviders;

		public PreDataModelProcessor(ImmutableArray<ICustomActionFactory> customActionProviders) => _customActionProviders = customActionProviders;

		public void Process(ref IExecutableEntity executableEntity)
		{
			Visit(ref executableEntity);
		}

		protected override void Build(ref ICustomAction customAction, ref CustomAction customActionProperties)
		{
			base.Build(ref customAction, ref customActionProperties);

			var customActionDispatcher = new CustomActionDispatcher(customActionProperties);
			customActionDispatcher.SetupExecutor(_customActionProviders);

			customAction = customActionDispatcher;
		}
	}
}