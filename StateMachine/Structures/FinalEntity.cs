using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct FinalEntity : IFinal, IVisitorEntity<FinalEntity, IFinal>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier?             Id       { get; set; }
		public ImmutableArray<IOnEntry> OnEntry  { get; set; }
		public ImmutableArray<IOnExit>  OnExit   { get; set; }
		public IDoneData?               DoneData { get; set; }

		void IVisitorEntity<FinalEntity, IFinal>.Init(IFinal source)
		{
			Ancestor = source;
			Id = source.Id;
			OnEntry = source.OnEntry;
			OnExit = source.OnExit;
			DoneData = source.DoneData;
		}

		bool IVisitorEntity<FinalEntity, IFinal>.RefEquals(in FinalEntity other) =>
				OnExit == other.OnExit &&
				OnEntry == other.OnEntry &&
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(DoneData, other.DoneData);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => @$"{Id}";
	}
}