using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class StateFluentBuilder<TOuterBuilder>
	{
		private readonly IStateBuilder           _builder;
		private readonly Action<IState>          _builtAction;
		private readonly IBuilderFactory         _factory;
		private readonly TOuterBuilder           _outerBuilder;
		private          List<IExecutableEntity> _onEntryList;
		private          List<IExecutableEntity> _onExitList;

		public StateFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<IState> builtAction)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_builder = factory.CreateStateBuilder();
			_outerBuilder = outerBuilder;
			_builtAction = builtAction;
		}

		public TOuterBuilder EndState()
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

		public StateFluentBuilder<TOuterBuilder> SetId(string id) => SetId((Identifier) id);

		public StateFluentBuilder<TOuterBuilder> SetId(IIdentifier id)
		{
			_builder.SetId(id);

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> SetInitial(params string[] initial)
		{
			_builder.SetInitial(IdentifierList.Create(initial, Identifier.FromString));

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> SetInitial(params IIdentifier[] initial)
		{
			_builder.SetInitial(IdentifierList.Create(initial));

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableAction action)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(action));

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableTask task)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(task));

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableCancellableTask task)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(task));

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> AddOnExit(ExecutableAction action)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(action));

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> AddOnExit(ExecutableTask task)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(task));

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> AddOnExit(ExecutableCancellableTask task)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(task));

			return this;
		}

		public InitialFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginInitial() => new InitialFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.SetInitial);

		public StateFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginState() => new StateFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddState);

		public ParallelFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginParallel() => new ParallelFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddParallel);

		public FinalFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginFinal() => new FinalFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddFinal);

		public HistoryFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginHistory() => new HistoryFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddHistory);

		public StateFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginState(string id) => BeginState((Identifier) id);

		public StateFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginState(IIdentifier id) =>
				new StateFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddState).SetId(id);

		public ParallelFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginParallel(string id) => BeginParallel((Identifier) id);

		public ParallelFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginParallel(IIdentifier id) =>
				new ParallelFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddParallel).SetId(id);

		public FinalFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginFinal(string id) => BeginFinal((Identifier) id);

		public FinalFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginFinal(IIdentifier id) =>
				new FinalFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddFinal).SetId(id);

		public HistoryFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginHistory(string id) => BeginHistory((Identifier) id);

		public HistoryFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginHistory(IIdentifier id) =>
				new HistoryFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddHistory).SetId(id);

		public TransitionFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginTransition() => new TransitionFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddTransition);

		public StateFluentBuilder<TOuterBuilder> AddTransition(EventDescriptor eventDescriptor, string target) => AddTransition(eventDescriptor, (Identifier) target);

		public StateFluentBuilder<TOuterBuilder> AddTransition(EventDescriptor eventDescriptor, IIdentifier target) => BeginTransition().SetEvent(eventDescriptor).SetTarget(target).EndTransition();

		public StateFluentBuilder<TOuterBuilder> AddTransition(Predicate condition, string target) => AddTransition(condition, (Identifier) target);

		public StateFluentBuilder<TOuterBuilder> AddTransition(Predicate condition, IIdentifier target) => BeginTransition().SetCondition(condition).SetTarget(target).EndTransition();
	}
}