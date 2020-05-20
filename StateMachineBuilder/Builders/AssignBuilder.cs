using System;

namespace Xtate
{
	public class AssignBuilder : BuilderBase, IAssignBuilder
	{
		private IValueExpression?    _expression;
		private string?              _inlineContent;
		private ILocationExpression? _location;

		public AssignBuilder(IErrorProcessor errorProcessor, object? ancestor) : base(errorProcessor, ancestor) { }

	#region Interface IAssignBuilder

		public IAssign Build() => new AssignEntity { Ancestor = Ancestor, Location = _location, Expression = _expression, InlineContent = _inlineContent };

		public void SetLocation(ILocationExpression location) => _location = location ?? throw new ArgumentNullException(nameof(location));

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		public void SetInlineContent(string inlineContent) => _inlineContent = inlineContent ?? throw new ArgumentNullException(nameof(inlineContent));

	#endregion
	}
}