using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class FinalFluentBuilder<TOuterBuilder>
	{
		private readonly IFinalBuilder           _builder;
		private readonly Action<IFinal>          _builtAction;
		private readonly TOuterBuilder           _outerBuilder;
		private          List<IExecutableEntity> _onEntryList;
		private          List<IExecutableEntity> _onExitList;

		public FinalFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<IFinal> builtAction)
		{
			_builder = factory.CreateFinalBuilder();
			_outerBuilder = outerBuilder;
			_builtAction = builtAction;
		}

		public TOuterBuilder EndFinal()
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

		public FinalFluentBuilder<TOuterBuilder> SetId(Identifier id)
		{
			_builder.SetId(id);

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableAction action)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(action));

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> AddOnEntryTask(ExecutableTask task)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(task));

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> AddOnExit(ExecutableAction action)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(action));

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> AddOnExitTask(ExecutableTask task)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(task));

			return this;
		}
	}
}