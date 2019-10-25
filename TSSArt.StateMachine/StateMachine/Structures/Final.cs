using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Final : IFinal, IEntity<Final, IFinal>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier             Id;
		public IReadOnlyList<IOnEntry> OnEntry;
		public IReadOnlyList<IOnExit>  OnExit;
		public IDoneData               DoneData;

		IIdentifier IFinal.Id => Id;

		IReadOnlyList<IOnEntry> IFinal.OnEntry => OnEntry;

		IReadOnlyList<IOnExit> IFinal.OnExit => OnExit;

		IDoneData IFinal.DoneData => DoneData;

		void IEntity<Final, IFinal>.Init(IFinal source)
		{
			Ancestor = source;
			Id = source.Id;
			OnEntry = source.OnEntry;
			OnExit = source.OnExit;
			DoneData = source.DoneData;
		}

		bool IEntity<Final, IFinal>.RefEquals(in Final other) =>
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(OnExit, other.OnExit) &&
				ReferenceEquals(OnEntry, other.OnEntry) &&
				ReferenceEquals(DoneData, other.DoneData);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}