using System;

namespace TSSArt.StateMachine
{
	public class ScriptBuilder : IScriptBuilder
	{
		private IScriptExpression         _body;
		private IExternalScriptExpression _source;

		public IScript Build()
		{
			if (_source != null && _body != null)
			{
				throw new InvalidOperationException(message: "Source and Body can't be used at the same time in Assign element");
			}

			return new Script { Source = _source, Content = _body };
		}

		public void SetSource(IExternalScriptExpression source) => _source = source ?? throw new ArgumentNullException(nameof(source));

		public void SetBody(IScriptExpression body) => _body = body ?? throw new ArgumentNullException(nameof(body));
	}
}