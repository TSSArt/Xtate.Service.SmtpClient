using System;

namespace TSSArt.StateMachine
{
	public struct ExternalDataExpression : IExternalDataExpression, IVisitorEntity<ExternalDataExpression, IExternalDataExpression>, IAncestorProvider
	{
		public Uri? Uri { get; set; }

		void IVisitorEntity<ExternalDataExpression, IExternalDataExpression>.Init(IExternalDataExpression source)
		{
			Ancestor = source;
			Uri = source.Uri;
		}

		bool IVisitorEntity<ExternalDataExpression, IExternalDataExpression>.RefEquals(in ExternalDataExpression other) => ReferenceEquals(Uri, other.Uri);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}