using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class DefaultContentBodyEvaluator : IContentBody, IStringEvaluator, IAncestorProvider
	{
		private readonly ContentBody _contentBody;

		public DefaultContentBodyEvaluator(in ContentBody contentBody)
		{
			Infrastructure.Assert(contentBody.Value != null);

			_contentBody = contentBody;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _contentBody.Ancestor;

	#endregion

	#region Interface IContentBody

		public string Value => _contentBody.Value!;

	#endregion

	#region Interface IStringEvaluator

		public virtual ValueTask<string> EvaluateString(IExecutionContext executionContext, CancellationToken token) => new ValueTask<string>(Value);

	#endregion
	}
}