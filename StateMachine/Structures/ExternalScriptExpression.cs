using System;

namespace TSSArt.StateMachine
{
	public struct ExternalScriptExpression : IExternalScriptExpression, IVisitorEntity<ExternalScriptExpression, IExternalScriptExpression>, IAncestorProvider
	{
		public Uri Uri { get; set; }

		void IVisitorEntity<ExternalScriptExpression, IExternalScriptExpression>.Init(IExternalScriptExpression source)
		{
			Ancestor = source;
			Uri = source.Uri;
		}

		bool IVisitorEntity<ExternalScriptExpression, IExternalScriptExpression>.RefEquals(in ExternalScriptExpression other) => ReferenceEquals(Uri, other.Uri);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}