using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal abstract class ExecutableEntityNode : IExecutableEntity, IExecEvaluator, IStoreSupport, IDocumentId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly IExecEvaluator      _execEvaluator;

		protected ExecutableEntityNode(LinkedListNode<int> documentIdNode, IExecutableEntity? entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_execEvaluator = entity.As<IExecEvaluator>();
			_documentIdNode = documentIdNode;
		}

		public int DocumentId => _documentIdNode.Value;

		public ValueTask Execute(IExecutionContext executionContext, CancellationToken token) => _execEvaluator.Execute(executionContext, token);

		void IStoreSupport.Store(Bucket bucket) => Store(bucket);

		protected abstract void Store(Bucket bucket);
	}
}