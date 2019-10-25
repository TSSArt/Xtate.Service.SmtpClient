using System;

namespace TSSArt.StateMachine
{
	public struct ExternalDataExpression : IExternalDataExpression, IEntity<ExternalDataExpression, IExternalDataExpression>, IAncestorProvider
	{
		public Uri Uri;

		Uri IExternalDataExpression.Uri => Uri;

		void IEntity<ExternalDataExpression, IExternalDataExpression>.Init(IExternalDataExpression source)
		{
			Ancestor = source;
			Uri = source.Uri;
		}

		bool IEntity<ExternalDataExpression, IExternalDataExpression>.RefEquals(in ExternalDataExpression other) => ReferenceEquals(Uri, other.Uri);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}