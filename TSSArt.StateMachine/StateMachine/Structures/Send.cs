using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Send : ISend, IEntity<Send, ISend>, IAncestorProvider, IDebugEntityId
	{
		public IContent                           Content;
		public IValueExpression                   DelayExpression;
		public int?                               DelayMs;
		public string                             Event;
		public IValueExpression                   EventExpression;
		public string                             Id;
		public ILocationExpression                IdLocation;
		public IReadOnlyList<ILocationExpression> NameList;
		public IReadOnlyList<IParam>              Parameters;
		public Uri                                Target;
		public IValueExpression                   TargetExpression;
		public Uri                                Type;
		public IValueExpression                   TypeExpression;

		IContent ISend.Content => Content;

		IValueExpression ISend.DelayExpression => DelayExpression;

		int? ISend.DelayMs => DelayMs;

		string ISend.Event => Event;

		IValueExpression ISend.EventExpression => EventExpression;

		string ISend.Id => Id;

		ILocationExpression ISend.IdLocation => IdLocation;

		IReadOnlyList<ILocationExpression> ISend.NameList => NameList;

		IReadOnlyList<IParam> ISend.Parameters => Parameters;

		Uri ISend.Target => Target;

		IValueExpression ISend.TargetExpression => TargetExpression;

		Uri ISend.Type => Type;

		IValueExpression ISend.TypeExpression => TypeExpression;

		void IEntity<Send, ISend>.Init(ISend source)
		{
			Ancestor = source;
			Id = source.Id;
			IdLocation = source.IdLocation;
			Event = source.Event;
			EventExpression = source.EventExpression;
			Content = source.Content;
			Target = source.Target;
			TargetExpression = source.TargetExpression;
			Type = source.Type;
			TypeExpression = source.TypeExpression;
			Parameters = source.Parameters;
			DelayMs = source.DelayMs;
			DelayExpression = source.DelayExpression;
			NameList = source.NameList;
		}

		bool IEntity<Send, ISend>.RefEquals(in Send other) =>
				DelayMs == other.DelayMs &&
				ReferenceEquals(DelayExpression, other.DelayExpression) &&
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(IdLocation, other.IdLocation) &&
				ReferenceEquals(Event, other.Event) &&
				ReferenceEquals(EventExpression, other.EventExpression) &&
				ReferenceEquals(Target, other.Target) &&
				ReferenceEquals(TargetExpression, other.TargetExpression) &&
				ReferenceEquals(Type, other.Type) &&
				ReferenceEquals(TypeExpression, other.TypeExpression) &&
				ReferenceEquals(Content, other.Content) &&
				ReferenceEquals(Parameters, other.Parameters) &&
				ReferenceEquals(NameList, other.NameList);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}