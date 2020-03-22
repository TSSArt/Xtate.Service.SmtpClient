using System;

namespace TSSArt.StateMachine
{
	public abstract class BuilderBase
	{
		private readonly IErrorProcessor _errorProcessor;

		protected BuilderBase(IErrorProcessor errorProcessor, object? ancestor)
		{
			_errorProcessor = errorProcessor;
			Ancestor = ancestor;
		}

		protected object? Ancestor { get; }

		protected void AddError(string message, Exception? exception = null)
		{
			_errorProcessor.AddError(GetType(), Ancestor, message, exception);
		}
	}
}