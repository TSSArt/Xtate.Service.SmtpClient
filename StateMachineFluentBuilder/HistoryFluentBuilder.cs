using System;

namespace TSSArt.StateMachine
{
	public class HistoryFluentBuilder<TOuterBuilder>
	{
		private readonly IHistoryBuilder  _builder;
		private readonly Action<IHistory> _builtAction;
		private readonly IBuilderFactory  _factory;
		private readonly TOuterBuilder    _outerBuilder;

		public HistoryFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<IHistory> builtAction)
		{
			_factory = factory;
			_builder = factory.CreateHistoryBuilder();
			_outerBuilder = outerBuilder;
			_builtAction = builtAction;
		}

		public TOuterBuilder EndHistory()
		{
			_builtAction(_builder.Build());
			return _outerBuilder;
		}

		public HistoryFluentBuilder<TOuterBuilder> SetId(Identifier id)
		{
			_builder.SetId(id);
			return this;
		}

		public HistoryFluentBuilder<TOuterBuilder> SetType(HistoryType type)
		{
			_builder.SetType(type);
			return this;
		}

		public TransitionFluentBuilder<HistoryFluentBuilder<TOuterBuilder>> BeginTransition() =>
				new TransitionFluentBuilder<HistoryFluentBuilder<TOuterBuilder>>(_factory, this, _builder.SetTransition);

		public HistoryFluentBuilder<TOuterBuilder> AddTransition(Identifier target) => BeginTransition().SetTarget(target).EndTransition();
	}
}