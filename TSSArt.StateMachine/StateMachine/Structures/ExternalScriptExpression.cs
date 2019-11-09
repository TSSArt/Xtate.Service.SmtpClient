using System;

namespace TSSArt.StateMachine
{
	public struct ExternalScriptExpression : IExternalScriptExpression, IEntity<ExternalScriptExpression, IExternalScriptExpression>, IAncestorProvider
	{
		public Uri Uri { get; set; }

		void IEntity<ExternalScriptExpression, IExternalScriptExpression>.Init(IExternalScriptExpression source)
		{
			Ancestor = source;
			Uri = source.Uri;
		}

		bool IEntity<ExternalScriptExpression, IExternalScriptExpression>.RefEquals(in ExternalScriptExpression other) => ReferenceEquals(Uri, other.Uri);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}