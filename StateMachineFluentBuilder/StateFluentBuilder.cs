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
			_factory = factory;
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

		public StateFluentBuilder<TOuterBuilder> SetId(Identifier id)
		{
			_builder.SetId(id);

			return this;
		}

		public StateFluentBuilder<TOuterBuilder> SetInitial(params Identifier[] initial)
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

		public StateFluentBuilder<TOuterBuilder> AddOnEntryTask(ExecutableTask task)
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

		public StateFluentBuilder<TOuterBuilder> AddOnExitTask(ExecutableTask task)
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

		public StateFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginState(Identifier id) =>
				new StateFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddState).SetId(id);

		public ParallelFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginParallel(Identifier id) =>
				new ParallelFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddParallel).SetId(id);

		public FinalFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginFinal(Identifier id) =>
				new FinalFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddFinal).SetId(id);

		public HistoryFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginHistory(Identifier id) =>
				new HistoryFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddHistory).SetId(id);

		public TransitionFluentBuilder<StateFluentBuilder<TOuterBuilder>> BeginTransition() => new TransitionFluentBuilder<StateFluentBuilder<TOuterBuilder>>(_factory, this, _builder.AddTransition);

		public StateFluentBuilder<TOuterBuilder> AddTransition(EventDescriptor eventDescriptor, Identifier target) => BeginTransition().SetEvent(eventDescriptor).SetTarget(target).EndTransition();

		public StateFluentBuilder<TOuterBuilder> AddTransition(Predicate condition, Identifier target) => BeginTransition().SetCondition(condition).SetTarget(target).EndTransition();
	}
}