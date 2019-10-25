using System;

namespace TSSArt.StateMachine
{
	public class DataBuilder : IDataBuilder
	{
		private IValueExpression        _expression;
		private string                  _id;
		private string                  _inlineContent;
		private IExternalDataExpression _source;

		public IData Build()
		{
			if (_inlineContent != null && _expression != null || _inlineContent != null && _source != null || _source != null && _expression != null)
			{
				throw new InvalidOperationException(message: "Expression and Source and Inline content can't be used at the same time in Data element");
			}

			return new Data { Id = _id, Source = _source, Expression = _expression, InlineContent = _inlineContent };
		}

		public void SetId(string id)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(id));

			_id = id;
		}

		public void SetSource(IExternalDataExpression source) => _source = source ?? throw new ArgumentNullException(nameof(source));

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		public void SetInlineContent(string inlineContent) => _inlineContent = inlineContent ?? throw new ArgumentNullException(nameof(inlineContent));
	}
}