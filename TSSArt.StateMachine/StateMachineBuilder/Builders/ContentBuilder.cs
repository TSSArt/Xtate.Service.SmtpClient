using System;

namespace TSSArt.StateMachine
{
	public class ContentBuilder : IContentBuilder
	{
		private IContentBody     _body;
		private IValueExpression _expression;

		public IContent Build()
		{
			if (_expression != null && _body != null)
			{
				throw new InvalidOperationException(message: "Expression and Body can't be used at the same time in Content element");
			}

			return new Content { Expression = _expression, Body = _body };
		}

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		public void SetBody(string body)
		{
			if (body == null) throw new ArgumentNullException(nameof(body));

			_body = new ContentBody { Value = body };
		}
	}
}