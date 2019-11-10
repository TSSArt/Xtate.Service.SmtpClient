using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class ParallelFluentBuilder<TOuterBuilder>
	{
		private readonly IParallelBuilder        _builder;
		private readonly Action<IParallel>       _builtAction;
		private readonly IBuilderFactory         _factory;
		private readonly TOuterBuilder           _outerBuilder;
		private          List<IExecutableEntity> _onEntryList;
		private          List<IExecutableEntity> _onExitList;

		public ParallelFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<IParallel> builtAction)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_builder = factory.CreateParallelBuilder();
			_outerBuilder = outerBuilder;
			_builtAction = builtAction;
		}

		public TOuterBuilder EndParallel()
		{
			if (_onEntryList != null)
			{
				_builder.AddOnEntry(new OnEntry { Action = ExecutableEntityList.Create(_onEntryList) });
			}

			if (_onExitList != null)
			{
				_builder.AddOnExit(new OnExit { Action = ExecutableEntityList.Create(_onExitList) });
			}

			_builtAction(_builder.Build());
			return _outerBuilder;
		}

		public ParallelFluentBuilder<TOuterBuilder> SetId(string id) => SetId((Identifier) id);

		public ParallelFluentBuilder<TOuterBuilder> SetId(IIdentifier id)
		{
			_builder.SetId(id);

			return this;
		}

		public ParallelFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableAction action)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(action));

			return this;
		}

		public ParallelFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableTask task)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(task));

			return this;
		}

		public ParallelFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableCancellableTask task)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(task));

			return this;
		}

		public ParallelFluentBuilder<TOuterBuilder> AddOnExit(ExecutableAction action)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(action));

			return this;
		}

		public ParallelFluentBuilder<TOuterBuilder> AddOnExit(ExecutableTask task)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(task));

			return this;
		}

		public ParallelFluentBuilder<TOuterBuilder> AddOnExit(ExecutableCancellableTask task)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(task));

			return this;
		}

		public StateFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginState() => new StateFluentBuilder<ParallelFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddState);

		public ParallelFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginParallel() => new ParallelFluentBuilder<ParallelFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddParallel);

		public HistoryFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginHistory() => new HistoryFluentBuilder<ParallelFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddHistory);

		public StateFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginState(string id) => BeginState((Identifier) id);

		public StateFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginState(IIdentifier id) =>
				new StateFluentBuilder<ParallelFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddState).SetId(id);

		public ParallelFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginParallel(string id) => BeginParallel((Identifier) id);

		public ParallelFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginParallel(IIdentifier id) =>
				new ParallelFluentBuilder<ParallelFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddParallel).SetId(id);

		public HistoryFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginHistory(string id) => BeginHistory((Identifier) id);

		public HistoryFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginHistory(IIdentifier id) =>
				new HistoryFluentBuilder<ParallelFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddHistory).SetId(id);

		public TransitionFluentBuilder<ParallelFluentBuilder<TOuterBuilder>> BeginTransition() =>
				new TransitionFluentBuilder<ParallelFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddTransition);

		public ParallelFluentBuilder<TOuterBuilder> AddTransition(EventDescriptor eventDescriptor, string target) => AddTransition(eventDescriptor, (Identifier) target);

		public ParallelFluentBuilder<TOuterBuilder> AddTransition(EventDescriptor eventDescriptor, IIdentifier target) => BeginTransition().SetEvent(eventDescriptor).SetTarget(target).EndTransition();

		public ParallelFluentBuilder<TOuterBuilder> AddTransition(Predicate condition, string target) => AddTransition(condition, (Identifier) target);

		public ParallelFluentBuilder<TOuterBuilder> AddTransition(Predicate condition, IIdentifier target) => BeginTransition().SetCondition(condition).SetTarget(target).EndTransition();
	}
}