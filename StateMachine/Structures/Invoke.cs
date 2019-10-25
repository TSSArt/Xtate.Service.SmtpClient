using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Invoke : IInvoke, IEntity<Invoke, IInvoke>, IAncestorProvider, IDebugEntityId
	{
		public bool                               AutoForward;
		public IContent                           Content;
		public IFinalize                          Finalize;
		public string                             Id;
		public ILocationExpression                IdLocation;
		public IReadOnlyList<ILocationExpression> NameList;
		public IReadOnlyList<IParam>              Parameters;
		public Uri                                Source;
		public IValueExpression                   SourceExpression;
		public Uri                                Type;
		public IValueExpression                   TypeExpression;

		bool IInvoke.AutoForward => AutoForward;

		IContent IInvoke.Content => Content;

		IFinalize IInvoke.Finalize => Finalize;

		string IInvoke.Id => Id;

		ILocationExpression IInvoke.IdLocation => IdLocation;

		IReadOnlyList<ILocationExpression> IInvoke.NameList => NameList;

		IReadOnlyList<IParam> IInvoke.Parameters => Parameters;

		Uri IInvoke.Source => Source;

		IValueExpression IInvoke.SourceExpression => SourceExpression;

		Uri IInvoke.Type => Type;

		IValueExpression IInvoke.TypeExpression => TypeExpression;

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
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(IdLocation, other.IdLocation) &&
				ReferenceEquals(Content, other.Content) &&
				ReferenceEquals(Type, other.Type) &&
				ReferenceEquals(TypeExpression, other.TypeExpression) &&
				ReferenceEquals(Source, other.Source) &&
				ReferenceEquals(SourceExpression, other.SourceExpression) &&
				ReferenceEquals(NameList, other.NameList) &&
				ReferenceEquals(Parameters, other.Parameters) &&
				ReferenceEquals(Finalize, other.Finalize);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}