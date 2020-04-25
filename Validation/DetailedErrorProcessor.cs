using System;
using System.Collections.Immutable;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public sealed class DetailedErrorProcessor : IErrorProcessor
	{
		private readonly string?        _scxml;
		private readonly string?        _sessionId;
		private readonly Uri?           _source;
		private readonly IStateMachine? _stateMachine;

		private ImmutableArray<ErrorItem>.Builder? _errors;

		public DetailedErrorProcessor(string? sessionId, IStateMachine? stateMachine, Uri? source, string? scxml)
		{
			_sessionId = sessionId;
			_stateMachine = stateMachine;
			_source = source;
			_scxml = scxml;
		}

	#region Interface IErrorProcessor

		public void ThrowIfErrors()
		{
			var errors = _errors;
			_errors = null;

			if (errors != null)
			{
				throw new StateMachineValidationException(errors.ToImmutable(), _sessionId, _stateMachine, _source, _scxml);
			}
		}

		void IErrorProcessor.AddError(ErrorItem errorItem)
		{
			if (errorItem == null) throw new ArgumentNullException(nameof(errorItem));

			(_errors ??= ImmutableArray.CreateBuilder<ErrorItem>()).Add(errorItem);
		}

		bool IErrorProcessor.LineInfoRequired => true;

	#endregion
	}
}