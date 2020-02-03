using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct State : IState, IEntity<State, IState>, IAncestorProvider, IDebugEntityId
	{
		public IDataModel                  DataModel     { get; set; }
		public IIdentifier                 Id            { get; set; }
		public IInitial                    Initial       { get; set; }
		public ImmutableArray<IInvoke>      Invoke        { get; set; }
		public ImmutableArray<IOnEntry>     OnEntry       { get; set; }
		public ImmutableArray<IOnExit>      OnExit        { get; set; }
		public ImmutableArray<IStateEntity> States        { get; set; }
		public ImmutableArray<IHistory>     HistoryStates { get; set; }
		public ImmutableArray<ITransition>  Transitions   { get; set; }

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
				ReferenceEquals(Initial, other.Initial) &&
				ReferenceEquals(DataModel, other.DataModel) &&
				Invoke == other.Invoke &&
				States == other.States &&
				HistoryStates == other.HistoryStates &&
				OnExit == other.OnExit &&
				OnEntry == other.OnEntry &&
				Transitions == other.Transitions;

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}