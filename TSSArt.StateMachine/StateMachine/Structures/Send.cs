using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Send : ISend, IEntity<Send, ISend>, IAncestorProvider, IDebugEntityId
	{
		public IContent                           Content          { get; set; }
		public IValueExpression                   DelayExpression  { get; set; }
		public int?                               DelayMs          { get; set; }
		public string                             Event            { get; set; }
		public IValueExpression                   EventExpression  { get; set; }
		public string                             Id               { get; set; }
		public ILocationExpression                IdLocation       { get; set; }
		public IReadOnlyList<ILocationExpression> NameList         { get; set; }
		public IReadOnlyList<IParam>              Parameters       { get; set; }
		public Uri                                Target           { get; set; }
		public IValueExpression                   TargetExpression { get; set; }
		public Uri                                Type             { get; set; }
		public IValueExpression                   TypeExpression   { get; set; }

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