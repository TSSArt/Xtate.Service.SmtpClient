using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal sealed class InvokeNode : IInvoke, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly ICancelInvokeEvaluator _cancelInvokeEvaluator;
		private readonly LinkedListNode<int>    _documentIdNode;
		private readonly Invoke                 _invoke;
		private readonly IStartInvokeEvaluator  _startInvokeEvaluator;
		private          string                 _stateId;

		public InvokeNode(LinkedListNode<int> documentIdNode, in Invoke invoke)
		{
			_documentIdNode = documentIdNode;
			_invoke = invoke;

			Finalize = invoke.Finalize.As<FinalizeNode>();
			_startInvokeEvaluator = invoke.As<IStartInvokeEvaluator>();
			_cancelInvokeEvaluator = invoke.As<ICancelInvokeEvaluator>();
		}

		public string InvokeId { get; private set; }

		public string InvokeUniqueId { get; private set; }

		public FinalizeNode Finalize { get; }

		object IAncestorProvider.Ancestor => _invoke.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		public Uri                                 Type             => _invoke.Type;
		public IValueExpression                    TypeExpression   => _invoke.TypeExpression;
		public Uri                                 Source           => _invoke.Source;
		public IValueExpression                    SourceExpression => _invoke.SourceExpression;
		public string                              Id               => _invoke.Id;
		public ILocationExpression                 IdLocation       => _invoke.IdLocation;
		public bool                                AutoForward      => _invoke.AutoForward;
		public ImmutableArray<ILocationExpression> NameList         => _invoke.NameList;
		public ImmutableArray<IParam>              Parameters       => _invoke.Parameters;
		public IContent                            Content          => _invoke.Content;
		IFinalize IInvoke.                         Finalize         => _invoke.Finalize;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.InvokeNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Id, Id);
			bucket.Add(Key.Type, Type);
			bucket.Add(Key.Source, Source);
			bucket.Add(Key.AutoForward, AutoForward);
			bucket.AddEntity(Key.TypeExpression, TypeExpression);
			bucket.AddEntity(Key.SourceExpression, SourceExpression);
			bucket.AddEntity(Key.IdLocation, IdLocation);
			bucket.AddEntityList(Key.NameList, NameList);
			bucket.AddEntityList(Key.Parameters, Parameters);
			bucket.AddEntity(Key.Finalize, Finalize);
			bucket.AddEntity(Key.Content, Content);
		}

		public void SetStateId(IIdentifier stateId) => _stateId = stateId.Base<IIdentifier>().ToString();

		public async ValueTask Start(IExecutionContext executionContext, CancellationToken token)
		{
			(InvokeId, InvokeUniqueId) = await _startInvokeEvaluator.Start(_stateId, executionContext, token).ConfigureAwait(false);
		}

		public async ValueTask Cancel(IExecutionContext executionContext, CancellationToken token)
		{
			var tmpInvokeId = InvokeId;
			InvokeId = null;
			InvokeUniqueId = null;

			if (tmpInvokeId != null)
			{
				await _cancelInvokeEvaluator.Cancel(tmpInvokeId, executionContext, token).ConfigureAwait(false);
			}
		}
	}
}