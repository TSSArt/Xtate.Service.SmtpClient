using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class StateBuilder : IStateBuilder
	{
		private readonly List<IHistory>             _historyStates = new List<IHistory>();
		private readonly List<IInvoke>              _invokeList    = new List<IInvoke>();
		private readonly List<IOnEntry>             _onEntryList   = new List<IOnEntry>();
		private readonly List<IOnExit>              _onExitList    = new List<IOnExit>();
		private readonly List<IStateEntity>         _states        = new List<IStateEntity>();
		private readonly List<ITransition>          _transitions   = new List<ITransition>();
		private          IDataModel                 _dataModel;
		private          IIdentifier                _id;
		private          IInitial                   _initial;
		private          /**/ImmutableArray<IIdentifier> _initialId;

		public IState Build()
		{
			if (_initialId != null)
			{
				if (_initial != null)
				{
					throw new InvalidOperationException(message: "Initial attribute and Initial state can't be used at the same time in State element");
				}

				_initial = new Initial { Transition = new Transition { Target = _initialId } };
			}

			if (_initial != null && _states.Count == 0)
			{
				throw new InvalidOperationException(message: "Initial state/property can be used only in complex (compound) states");
			}

			return new State
				   {
						   Id = _id, Initial = _initial, States = StateEntityList.Create(_states), HistoryStates = HistoryList.Create(_historyStates),
						   Transitions = TransitionList.Create(_transitions), DataModel = _dataModel, OnEntry = OnEntryList.Create(_onEntryList),
						   OnExit = OnExitList.Create(_onExitList), Invoke = InvokeList.Create(_invokeList)
				   };
		}

		public void SetId(IIdentifier id) => _id = id ?? throw new ArgumentNullException(nameof(id));

		public void SetInitial(/**/ImmutableArray<IIdentifier> initialId) => _initialId = IdentifierList.Create(initialId ?? throw new ArgumentNullException(nameof(initialId)));

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

		public void SetInitial(IInitial initial) => _initial = initial ?? throw new ArgumentNullException(nameof(initial));

		public void AddFinal(IFinal final)
		{
			if (final == null) throw new ArgumentNullException(nameof(final));

			_states.Add(final);
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