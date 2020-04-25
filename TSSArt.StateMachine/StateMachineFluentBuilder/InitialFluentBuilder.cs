using System;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class InitialFluentBuilder<TOuterBuilder>
	{
		private readonly IInitialBuilder  _builder;
		private readonly Action<IInitial> _builtAction;
		private readonly IBuilderFactory  _factory;
		private readonly TOuterBuilder    _outerBuilder;

		public InitialFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<IInitial> builtAction)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_builder = factory.CreateInitialBuilder(null);
			_outerBuilder = outerBuilder;
			_builtAction = builtAction;
		}

		public TOuterBuilder EndInitial()
		{
			_builtAction(_builder.Build());

			return _outerBuilder;
		}

		public TransitionFluentBuilder<InitialFluentBuilder<TOuterBuilder>> BeginTransition() =>
				new TransitionFluentBuilder<InitialFluentBuilder<TOuterBuilder>>(_factory, this, _builder.SetTransition);

		public InitialFluentBuilder<TOuterBuilder> AddTransition(string target) => AddTransition((Identifier) target);

		public InitialFluentBuilder<TOuterBuilder> AddTransition(IIdentifier target) => BeginTransition().SetTarget(target).EndTransition();
	}
}