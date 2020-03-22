using System;

namespace TSSArt.StateMachine
{
	public class ContentBuilder : BuilderBase, IContentBuilder
	{
		private IContentBody?     _body;
		private IValueExpression? _expression;

		public ContentBuilder(IErrorProcessor errorProcessor, object? ancestor) : base(errorProcessor, ancestor)
		{ }

		public IContent Build() => new ContentEntity { Ancestor = Ancestor, Expression = _expression, Body = _body };

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		public void SetBody(string body)
		{
			if (body == null) throw new ArgumentNullException(nameof(body));

			_body = new ContentBody { Value = body };
		}
	}
}