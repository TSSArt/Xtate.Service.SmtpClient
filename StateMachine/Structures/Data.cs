using System;

namespace TSSArt.StateMachine
{
	public struct Data : IData, IEntity<Data, IData>, IAncestorProvider, IDebugEntityId
	{
		public string                  Id            { get; set; }
		public IExternalDataExpression Source        { get; set; }
		public IValueExpression        Expression    { get; set; }
		public string                  InlineContent { get; set; }

		void IEntity<Data, IData>.Init(IData source)
		{
			Ancestor = source;
			Id = source.Id;
			Source = source.Source;
			Expression = source.Expression;
			InlineContent = source.InlineContent;
		}

		bool IEntity<Data, IData>.RefEquals(in Data other) =>
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(Source, other.Source) &&
				ReferenceEquals(InlineContent, other.InlineContent) &&
				ReferenceEquals(Expression, other.Expression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}