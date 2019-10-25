using System;

namespace TSSArt.StateMachine
{
	public class AssignBuilder : IAssignBuilder
	{
		private IValueExpression    _expression;
		private string              _inlineContent;
		private ILocationExpression _location;

		public IAssign Build()
		{
			if (_expression != null && _inlineContent != null)
			{
				throw new InvalidOperationException(message: "Expression and Inline content can't be used at the same time in Assign element");
			}

			return new Assign { Location = _location, Expression = _expression, InlineContent = _inlineContent };
		}

		public void SetLocation(ILocationExpression location) => _location = location ?? throw new ArgumentNullException(nameof(location));

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		public void SetInlineContent(string inlineContent) => _inlineContent = inlineContent ?? throw new ArgumentNullException(nameof(inlineContent));
	}
}