using System;

namespace TSSArt.StateMachine
{
	public struct DataEntity : IData, IVisitorEntity<DataEntity, IData>, IAncestorProvider, IDebugEntityId
	{
		public string?                  Id            { get; set; }
		public IExternalDataExpression? Source        { get; set; }
		public IValueExpression?        Expression    { get; set; }
		public string?                  InlineContent { get; set; }

		void IVisitorEntity<DataEntity, IData>.Init(IData source)
		{
			Ancestor = source;
			Id = source.Id;
			Source = source.Source;
			Expression = source.Expression;
			InlineContent = source.InlineContent;
		}

		bool IVisitorEntity<DataEntity, IData>.RefEquals(in DataEntity other) =>
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(Source, other.Source) &&
				ReferenceEquals(InlineContent, other.InlineContent) &&
				ReferenceEquals(Expression, other.Expression);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => @$"{Id}";
	}
}