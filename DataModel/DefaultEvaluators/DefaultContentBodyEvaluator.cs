using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultContentBodyEvaluator : IContentBody, IObjectEvaluator, IAncestorProvider
	{
		private readonly ContentBody _contentBody;

		public DefaultContentBodyEvaluator(in ContentBody contentBody)
		{
			_contentBody = contentBody;
		}

		public string Value => _contentBody.Value;

		object IAncestorProvider.Ancestor => _contentBody.Ancestor;

		public virtual ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			return new ValueTask<IObject>(new DataModelValue(Value));
		}
	}
}