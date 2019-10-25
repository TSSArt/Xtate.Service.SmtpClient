using System;

namespace TSSArt.StateMachine
{
	public class ContentBuilder : IContentBuilder
	{
		private string           _body;
		private IValueExpression _expression;

		public IContent Build()
		{
			if (_expression != null && _body != null)
			{
				throw new InvalidOperationException(message: "Expression and Body can't be used at the same time in Content element");
			}

			return new Content { Expression = _expression, Value = _body };
		}

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		public void SetBody(string body) => _body = body ?? throw new ArgumentNullException(nameof(body));
	}
}