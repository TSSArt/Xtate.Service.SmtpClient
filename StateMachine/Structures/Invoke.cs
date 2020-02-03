using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public struct Invoke : IInvoke, IEntity<Invoke, IInvoke>, IAncestorProvider, IDebugEntityId
	{
		public bool                               AutoForward      { get; set; }
		public IContent                           Content          { get; set; }
		public IFinalize                          Finalize         { get; set; }
		public string                             Id               { get; set; }
		public ILocationExpression                IdLocation       { get; set; }
		public /**/ImmutableArray<ILocationExpression> NameList         { get; set; }
		public /**/ImmutableArray<IParam>              Parameters       { get; set; }
		public Uri                                Source           { get; set; }
		public IValueExpression                   SourceExpression { get; set; }
		public Uri                                Type             { get; set; }
		public IValueExpression                   TypeExpression   { get; set; }

		void IEntity<Invoke, IInvoke>.Init(IInvoke source)
		{
			Ancestor = source;
			Id = source.Id;
			IdLocation = source.IdLocation;
			Content = source.Content;
			Type = source.Type;
			TypeExpression = source.TypeExpression;
			Source = source.Source;
			SourceExpression = source.SourceExpression;
			NameList = source.NameList;
			Parameters = source.Parameters;
			Finalize = source.Finalize;
			AutoForward = source.AutoForward;
		}

		bool IEntity<Invoke, IInvoke>.RefEquals(in Invoke other) =>
				AutoForward == other.AutoForward &&
				NameList == other.NameList &&
				Parameters == other.Parameters &&
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(IdLocation, other.IdLocation) &&
				ReferenceEquals(Content, other.Content) &&
				ReferenceEquals(Type, other.Type) &&
				ReferenceEquals(TypeExpression, other.TypeExpression) &&
				ReferenceEquals(Source, other.Source) &&
				ReferenceEquals(SourceExpression, other.SourceExpression) &&
				ReferenceEquals(Finalize, other.Finalize);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}