using System;

namespace TSSArt.StateMachine
{
	public class ParamBuilder : IParamBuilder
	{
		private IValueExpression    _expression;
		private ILocationExpression _location;
		private string              _name;

		public IParam Build()
		{
			if (_expression != null && _location != null)
			{
				throw new InvalidOperationException(message: "Expression and Location can't be used at the same time in Param element");
			}

			return new Param { Name = _name, Expression = _expression, Location = _location };
		}

		public void SetName(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			_name = name;
		}

		public void SetExpression(IValueExpression expression) => _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		public void SetLocation(ILocationExpression location) => _location = location ?? throw new ArgumentNullException(nameof(location));
	}
}