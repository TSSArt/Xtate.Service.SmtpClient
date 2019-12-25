using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultContentBodyEvaluator : IContentBody, IStringEvaluator, IAncestorProvider
	{
		private readonly ContentBody _contentBody;

		public DefaultContentBodyEvaluator(in ContentBody contentBody) => _contentBody = contentBody;

		object IAncestorProvider.Ancestor => _contentBody.Ancestor;

		public string Value => _contentBody.Value;

		public virtual ValueTask<string> EvaluateString(IExecutionContext executionContext, CancellationToken token) => new ValueTask<string>(Value);
	}
}