using System;

namespace TSSArt.StateMachine
{
	internal sealed class ExternalDataExpressionNode : IExternalDataExpression, IStoreSupport, IAncestorProvider
	{
		private readonly ExternalDataExpression _externalDataExpression;

		public ExternalDataExpressionNode(in ExternalDataExpression externalDataExpression)
		{
			Infrastructure.Assert(externalDataExpression.Uri != null);

			_externalDataExpression = externalDataExpression;
		}

		object? IAncestorProvider.Ancestor => _externalDataExpression.Ancestor;

		public Uri Uri => _externalDataExpression.Uri!;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ExternalDataExpressionNode);
			bucket.Add(Key.Uri, Uri);
		}
	}
}