using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class WrapperErrorProcessor : IErrorProcessor
	{
		private readonly IErrorProcessor? _errorProcessor;

		private ErrorItem? _error;

		public WrapperErrorProcessor(IErrorProcessor? errorProcessor) => _errorProcessor = errorProcessor;

		public void AddError(ErrorItem errorItem)
		{
			if (errorItem == null) throw new ArgumentNullException(nameof(errorItem));

			if (_error == null)
			{
				_error = errorItem;
			}

			_errorProcessor?.AddError(errorItem);
		}

		public bool LineInfoRequired => _errorProcessor?.LineInfoRequired ?? false;

		public void ThrowIfErrors()
		{
			if (_error != null)
			{
				throw new StateMachineValidationException(ImmutableArray.Create(_error));
			}
		}
	}
}