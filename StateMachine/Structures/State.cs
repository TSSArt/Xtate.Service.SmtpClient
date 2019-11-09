using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct State : IState, IEntity<State, IState>, IAncestorProvider, IDebugEntityId
	{
		public IDataModel                  DataModel     { get; set; }
		public IIdentifier                 Id            { get; set; }
		public IInitial                    Initial       { get; set; }
		public IReadOnlyList<IInvoke>      Invoke        { get; set; }
		public IReadOnlyList<IOnEntry>     OnEntry       { get; set; }
		public IReadOnlyList<IOnExit>      OnExit        { get; set; }
		public IReadOnlyList<IStateEntity> States        { get; set; }
		public IReadOnlyList<IHistory>     HistoryStates { get; set; }
		public IReadOnlyList<ITransition>  Transitions   { get; set; }

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