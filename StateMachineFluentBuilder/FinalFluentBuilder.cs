using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class FinalFluentBuilder<TOuterBuilder>
	{
		private readonly IFinalBuilder           _builder;
		private readonly Action<IFinal>          _builtAction;
		private readonly IBuilderFactory         _factory;
		private readonly TOuterBuilder           _outerBuilder;
		private          IValueExpression        _contentExpression;
		private          List<IExecutableEntity> _onEntryList;
		private          List<IExecutableEntity> _onExitList;

		public FinalFluentBuilder(IBuilderFactory factory, TOuterBuilder outerBuilder, Action<IFinal> builtAction)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
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

			if (_contentExpression != null)
			{
				var contentBuilder = _factory.CreateContentBuilder();
				contentBuilder.SetExpression(_contentExpression);

				var doneData = _factory.CreateDoneDataBuilder();
				doneData.SetContent(contentBuilder.Build());

				_builder.SetDoneData(doneData.Build());
			}

			_builtAction(_builder.Build());

			return _outerBuilder;
		}

		public FinalFluentBuilder<TOuterBuilder> SetId(string id) => SetId((Identifier) id);

		public FinalFluentBuilder<TOuterBuilder> SetId(IIdentifier id)
		{
			_builder.SetId(id);

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> SetDoneData(Evaluator evaluator)
		{
			_contentExpression = new RuntimeEvaluator(evaluator);

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> SetDoneData(EvaluatorTask task)
		{
			_contentExpression = new RuntimeEvaluator(task);

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> SetDoneData(EvaluatorCancellableTask task)
		{
			_contentExpression = new RuntimeEvaluator(task);

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

		public FinalFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableTask task)
		{
			if (_onEntryList == null)
			{
				_onEntryList = new List<IExecutableEntity>();
			}

			_onEntryList.Add(new RuntimeAction(task));

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> AddOnEntry(ExecutableCancellableTask task)
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

		public FinalFluentBuilder<TOuterBuilder> AddOnExit(ExecutableTask task)
		{
			if (_onExitList == null)
			{
				_onExitList = new List<IExecutableEntity>();
			}

			_onExitList.Add(new RuntimeAction(task));

			return this;
		}

		public FinalFluentBuilder<TOuterBuilder> AddOnExit(ExecutableCancellableTask task)
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