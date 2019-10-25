using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Parallel : IParallel, IEntity<Parallel, IParallel>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier                 Id;
		public IReadOnlyList<IStateEntity> States;
		public IReadOnlyList<IHistory>     HistoryStates;
		public IReadOnlyList<ITransition>  Transitions;
		public IDataModel                  DataModel;
		public IReadOnlyList<IOnEntry>     OnEntry;
		public IReadOnlyList<IOnExit>      OnExit;
		public IReadOnlyList<IInvoke>      Invoke;

		IIdentifier IParallel.Id => Id;

		IReadOnlyList<IStateEntity> IParallel.States => States;

		IReadOnlyList<IHistory> IParallel.HistoryStates => HistoryStates;

		IReadOnlyList<ITransition> IParallel.Transitions => Transitions;

		IDataModel IParallel.DataModel => DataModel;

		IReadOnlyList<IOnEntry> IParallel.OnEntry => OnEntry;

		IReadOnlyList<IOnExit> IParallel.OnExit => OnExit;

		IReadOnlyList<IInvoke> IParallel.Invoke => Invoke;

		void IEntity<Parallel, IParallel>.Init(IParallel source)
		{
			Ancestor = source;
			Id = source.Id;
			Invoke = source.Invoke;
			States = source.States;
			HistoryStates = source.HistoryStates;
			DataModel = source.DataModel;
			OnExit = source.OnExit;
			OnEntry = source.OnEntry;
			Transitions = source.Transitions;
		}

		bool IEntity<Parallel, IParallel>.RefEquals(in Parallel other) =>
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(Invoke, other.Invoke) &&
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