using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Parallel : IParallel, IEntity<Parallel, IParallel>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier                 Id            { get; set; }
		public IReadOnlyList<IStateEntity> States        { get; set; }
		public IReadOnlyList<IHistory>     HistoryStates { get; set; }
		public IReadOnlyList<ITransition>  Transitions   { get; set; }
		public IDataModel                  DataModel     { get; set; }
		public IReadOnlyList<IOnEntry>     OnEntry       { get; set; }
		public IReadOnlyList<IOnExit>      OnExit        { get; set; }
		public IReadOnlyList<IInvoke>      Invoke        { get; set; }

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