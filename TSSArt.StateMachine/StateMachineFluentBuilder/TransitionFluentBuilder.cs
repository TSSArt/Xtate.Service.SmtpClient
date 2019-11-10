using System;

namespace TSSArt.StateMachine
{
	public class TransitionFluentBuilder<TOuterBuilder>
	{
		private readonly ITransitionBuilder  _builder;
		private readonly Action<ITransition> _builtAction;
		private readonly TOuterBuilder       _outerBuilder;

		public TransitionFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<ITransition> builtAction)
		{
			if (factory == null) throw new ArgumentNullException(nameof(factory));

			_builder = factory.CreateTransitionBuilder();
			_outerBuilder = outerBuilder;
			_builtAction = builtAction;
		}

		public TOuterBuilder EndTransition()
		{
			_builtAction(_builder.Build());
			return _outerBuilder;
		}

		public TransitionFluentBuilder<TOuterBuilder> SetEvent(params EventDescriptor[] eventsDescriptor)
		{
			_builder.SetEvent(EventDescriptorList.Create(eventsDescriptor));
			return this;
		}

		public TransitionFluentBuilder<TOuterBuilder> SetCondition(PredicateCancellableTask predicate)
		{
			_builder.SetCondition(new RuntimePredicate(predicate));
			return this;
		}

		public TransitionFluentBuilder<TOuterBuilder> SetCondition(Predicate predicate)
		{
			_builder.SetCondition(new RuntimePredicate(predicate));
			return this;
		}

		public TransitionFluentBuilder<TOuterBuilder> SetTarget(params string[] target)
		{
			_builder.SetTarget(IdentifierList.Create(target, Identifier.FromString));
			return this;
		}

		public TransitionFluentBuilder<TOuterBuilder> SetTarget(params IIdentifier[] target)
		{
			_builder.SetTarget(IdentifierList.Create(target));
			return this;
		}

		public TransitionFluentBuilder<TOuterBuilder> SetType(TransitionType type)
		{
			_builder.SetType(type);
			return this;
		}

		public TransitionFluentBuilder<TOuterBuilder> AddOnTransition(ExecutableAction action)
		{
			_builder.AddAction(new RuntimeAction(action));
			return this;
		}

		public TransitionFluentBuilder<TOuterBuilder> AddOnTransitionTask(ExecutableTask task)
		{
			_builder.AddAction(new RuntimeAction(task));
			return this;
		}
	}
}