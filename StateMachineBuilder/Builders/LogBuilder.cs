using System;

namespace TSSArt.StateMachine
{
	public class LogBuilder : ILogBuilder
	{
		private IValueExpression _expression;
		private string           _label;

		public ILog Build() => new Log { Label = _label, Expression = _expression };

		public void SetLabel(string label)
		{
			if (string.IsNullOrEmpty(label)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(label));

			_label = label;
		}

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));
	}
}