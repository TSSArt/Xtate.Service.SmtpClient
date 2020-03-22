using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

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

		object? IAncestorProvider.Ancestor => _contentBody.Ancestor;

		public string Value => _contentBody.Value!;

		public virtual ValueTask<string> EvaluateString(IExecutionContext executionContext, CancellationToken token) => new ValueTask<string>(Value);
	}
}