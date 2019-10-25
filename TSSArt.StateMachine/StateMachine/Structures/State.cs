using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct State : IState, IEntity<State, IState>, IAncestorProvider, IDebugEntityId
	{
		public IDataModel                  DataModel;
		public IIdentifier                 Id;
		public IInitial                    Initial;
		public IReadOnlyList<IInvoke>      Invoke;
		public IReadOnlyList<IOnEntry>     OnEntry;
		public IReadOnlyList<IOnExit>      OnExit;
		public IReadOnlyList<IStateEntity> States;
		public IReadOnlyList<IHistory>     HistoryStates;
		public IReadOnlyList<ITransition>  Transitions;

		IIdentifier IState.Id => Id;

		IInitial IState.Initial => Initial;

		IReadOnlyList<IStateEntity> IState.States => States;

		IReadOnlyList<IHistory> IState.HistoryStates => HistoryStates;

		IReadOnlyList<ITransition> IState.Transitions => Transitions;

		IDataModel IState.DataModel => DataModel;

		IReadOnlyList<IOnEntry> IState.OnEntry => OnEntry;

		IReadOnlyList<IOnExit> IState.OnExit => OnExit;

		IReadOnlyList<IInvoke> IState.Invoke => Invoke;

		void IEntity<State, IState>.Init(IState source)
		{
			Ancestor = source;
			Id = source.Id;
			Invoke = source.Invoke;
			Initial = source.Initial;
			States = source.States;
			HistoryStates = source.HistoryStates;
			DataModel = source.DataModel;
			OnExit = source.OnExit;
			OnEntry = source.OnEntry;
			Transitions = source.Transitions;
		}

		bool IEntity<State, IState>.RefEquals(in State other) =>
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(Invoke, other.Invoke) &&
				ReferenceEquals(Initial, other.Initial) &&
				ReferenceEquals(DataModel, other.DataModel) &&
				ReferenceEquals(States, other.States) &&
				ReferenceEquals(HistoryStates, other.HistoryStates) &&
				ReferenceEquals(OnExit, other.OnExit) &&
				ReferenceEquals(OnEntry, other.OnEntry) &&
				ReferenceEquals(Transitions, other.Transitions);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}