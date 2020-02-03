using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public struct Parallel : IParallel, IEntity<Parallel, IParallel>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier                 Id            { get; set; }
		public /**/ImmutableArray<IStateEntity> States        { get; set; }
		public /**/ImmutableArray<IHistory>     HistoryStates { get; set; }
		public /**/ImmutableArray<ITransition>  Transitions   { get; set; }
		public IDataModel                  DataModel     { get; set; }
		public /**/ImmutableArray<IOnEntry>     OnEntry       { get; set; }
		public /**/ImmutableArray<IOnExit>      OnExit        { get; set; }
		public /**/ImmutableArray<IInvoke>      Invoke        { get; set; }

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