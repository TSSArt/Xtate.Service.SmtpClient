using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct Final : IFinal, IEntity<Final, IFinal>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier             Id       { get; set; }
		public ImmutableArray<IOnEntry> OnEntry  { get; set; }
		public ImmutableArray<IOnExit>  OnExit   { get; set; }
		public IDoneData               DoneData { get; set; }

		void IEntity<Final, IFinal>.Init(IFinal source)
		{
			Ancestor = source;
			Id = source.Id;
			OnEntry = source.OnEntry;
			OnExit = source.OnExit;
			DoneData = source.DoneData;
		}

		bool IEntity<Final, IFinal>.RefEquals(in Final other) =>
				OnExit == other.OnExit &&
				OnEntry == other.OnEntry &&
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(DoneData, other.DoneData);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}