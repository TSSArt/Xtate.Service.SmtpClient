using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class ParallelBuilder : IParallelBuilder
	{
		private readonly List<IHistory>     _historyStates = new List<IHistory>();
		private readonly List<IInvoke>      _invokeList    = new List<IInvoke>();
		private readonly List<IOnEntry>     _onEntryList   = new List<IOnEntry>();
		private readonly List<IOnExit>      _onExitList    = new List<IOnExit>();
		private readonly List<IStateEntity> _states        = new List<IStateEntity>();
		private readonly List<ITransition>  _transitions   = new List<ITransition>();
		private          IDataModel         _dataModel;
		private          IIdentifier        _id;

		public IParallel Build() =>
				new Parallel
				{
						Id = _id, States = StateEntityList.Create(_states), HistoryStates = HistoryList.Create(_historyStates),
						Transitions = TransitionList.Create(_transitions), DataModel = _dataModel, OnEntry = OnEntryList.Create(_onEntryList),
						OnExit = OnExitList.Create(_onExitList), Invoke = InvokeList.Create(_invokeList)
				};

		public void SetId(IIdentifier id) => _id = id ?? throw new ArgumentNullException(nameof(id));

		public void AddState(IState state)
		{
			if (state == null) throw new ArgumentNullException(nameof(state));

			_states.Add(state);
		}

		public void AddParallel(IParallel parallel)
		{
			if (parallel == null) throw new ArgumentNullException(nameof(parallel));

			_states.Add(parallel);
		}

		public void AddHistory(IHistory history)
		{
			if (history == null) throw new ArgumentNullException(nameof(history));

			_historyStates.Add(history);
		}

		public void AddTransition(ITransition transition)
		{
			if (transition == null) throw new ArgumentNullException(nameof(transition));

			_transitions.Add(transition);
		}

		public void AddOnEntry(IOnEntry onEntry)
		{
			if (onEntry == null) throw new ArgumentNullException(nameof(onEntry));

			_onEntryList.Add(onEntry);
		}

		public void AddOnExit(IOnExit onExit)
		{
			if (onExit == null) throw new ArgumentNullException(nameof(onExit));

			_onExitList.Add(onExit);
		}

		public void AddInvoke(IInvoke invoke)
		{
			if (invoke == null) throw new ArgumentNullException(nameof(invoke));

			_invokeList.Add(invoke);
		}

		public void SetDataModel(IDataModel dataModel) => _dataModel = dataModel ?? throw new ArgumentNullException(nameof(dataModel));
	}
}