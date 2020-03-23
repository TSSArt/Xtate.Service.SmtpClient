using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public sealed class DetailedErrorProcessor : IErrorProcessor
	{
		private ImmutableArray<ErrorItem>.Builder? _errors;

	#region Interface IErrorProcessor

		public void ThrowIfErrors()
		{
			if (_errors != null)
			{
				throw new StateMachineValidationException(_errors.ToImmutable());
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