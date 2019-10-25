using System;

namespace TSSArt.StateMachine
{
	public class InitialFluentBuilder<TOuterBuilder>
	{
		private readonly IInitialBuilder  _builder;
		private readonly Action<IInitial> _builtAction;
		private readonly IBuilderFactory  _factory;
		private readonly TOuterBuilder    _outerBuilder;

		public InitialFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<IInitial> builtAction)
		{
			_factory = factory;
			_builder = factory.CreateInitialBuilder();
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

		public InitialFluentBuilder<TOuterBuilder> AddTransition(Identifier target) => BeginTransition().SetTarget(target).EndTransition();
	}
}