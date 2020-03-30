using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal abstract class ExecutableEntityNode : IExecutableEntity, IExecEvaluator, IStoreSupport, IDocumentId
	{
		private          DocumentIdRecord _documentIdNode;
		private readonly IExecEvaluator   _execEvaluator;

		protected ExecutableEntityNode(in DocumentIdRecord documentIdNode, IExecutableEntity? entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_execEvaluator = entity.As<IExecEvaluator>();
			_documentIdNode = documentIdNode;
		}

	#region Interface IDocumentId

		public int DocumentId => _documentIdNode.Value;

	#endregion

	#region Interface IExecEvaluator

		public ValueTask Execute(IExecutionContext executionContext, CancellationToken token) => _execEvaluator.Execute(executionContext, token);

	#endregion

	#region Interface IStoreSupport

		void IStoreSupport.Store(Bucket bucket) => Store(bucket);

	#endregion

		protected abstract void Store(Bucket bucket);
	}
}