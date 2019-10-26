using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class InvokeNode : IInvoke, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly IObjectEvaluator                  _contentExpressionEvaluator;
		private readonly LinkedListNode<int>               _documentIdNode;
		private readonly ILocationEvaluator                _idLocationEvaluator;
		private readonly Invoke                            _invoke;
		private readonly IReadOnlyList<ILocationEvaluator> _nameEvaluatorList;
		private readonly IReadOnlyList<DefaultParam>       _parameterList;
		private readonly IStringEvaluator                  _sourceExpressionEvaluator;
		private readonly IStringEvaluator                  _typeExpressionEvaluator;
		private          IIdentifier                       _stateId;

		public InvokeNode(LinkedListNode<int> documentIdNode, in Invoke invoke)
		{
			_documentIdNode = documentIdNode;
			_invoke = invoke;

			Finalize = invoke.Finalize.As<FinalizeNode>();
			_typeExpressionEvaluator = invoke.TypeExpression.As<IStringEvaluator>();
			_sourceExpressionEvaluator = invoke.SourceExpression.As<IStringEvaluator>();
			_contentExpressionEvaluator = invoke.Content?.Expression.As<IObjectEvaluator>();
			_idLocationEvaluator = invoke.IdLocation.As<ILocationEvaluator>();
			_nameEvaluatorList = invoke.NameList.AsListOf<ILocationEvaluator>();
			_parameterList = invoke.Parameters.AsListOf<DefaultParam>();
		}

		public string InvokeId { get; private set; }

		public FinalizeNode Finalize { get; }

		object IAncestorProvider.Ancestor => _invoke.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		public Uri                                Type             => _invoke.Type;
		public IValueExpression                   TypeExpression   => _invoke.TypeExpression;
		public Uri                                Source           => _invoke.Source;
		public IValueExpression                   SourceExpression => _invoke.SourceExpression;
		public string                             Id               => _invoke.Id;
		public ILocationExpression                IdLocation       => _invoke.IdLocation;
		public bool                               AutoForward      => _invoke.AutoForward;
		public IReadOnlyList<ILocationExpression> NameList         => _invoke.NameList;
		public IReadOnlyList<IParam>              Parameters       => _invoke.Parameters;
		public IContent                           Content          => _invoke.Content;
		IFinalize IInvoke.                        Finalize         => _invoke.Finalize;

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

		public void SetStateId(IIdentifier stateId) => _stateId = stateId.Base<IIdentifier>();

		public async ValueTask Start(string sessionId, ExternalCommunicationWrapper externalCommunication, IExecutionContext executionContext, CancellationToken token)
		{
			InvokeId = _invoke.Id ?? IdGenerator.NewInvokeId(_stateId.ToString());

			var type = _typeExpressionEvaluator != null ? ToUri(await _typeExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _invoke.Type;
			var source = _sourceExpressionEvaluator != null ? ToUri(await _sourceExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false)) : _invoke.Source;

			var data = await Converter.GetData(_invoke.Content?.Value, _contentExpressionEvaluator, _nameEvaluatorList, _parameterList, executionContext, token).ConfigureAwait(false);

			await externalCommunication.StartInvoke(sessionId, InvokeId, type, source, data, token);

			_idLocationEvaluator?.SetValue(new DefaultObject(InvokeId), executionContext);
		}

		public async ValueTask Cancel(string sessionId, ExternalCommunicationWrapper externalCommunication, IExecutionContext executionContext, CancellationToken token)
		{
			var tmpInvokeId = InvokeId;
			InvokeId = null;

			await externalCommunication.CancelInvoke(sessionId, tmpInvokeId, token);
		}

		private static Uri ToUri(string uri) => new Uri(uri, UriKind.RelativeOrAbsolute);
	}
}