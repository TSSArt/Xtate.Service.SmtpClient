namespace TSSArt.StateMachine
{
	internal sealed class ContentNode : IContent, IStoreSupport, IAncestorProvider
	{
		private readonly Content _content;

		public ContentNode(in Content content) => _content = content;

		object IAncestorProvider.Ancestor => _content.Ancestor;

		public IValueExpression Expression => _content.Expression;

		public IContentBody Body => _content.Body;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ContentNode);
			bucket.AddEntity(Key.Expression, Expression);
			bucket.Add(Key.Body, Body?.Value);
		}
	}
}