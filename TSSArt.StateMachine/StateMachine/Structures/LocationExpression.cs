﻿namespace TSSArt.StateMachine
{
	public struct LocationExpression : ILocationExpression, IVisitorEntity<LocationExpression, ILocationExpression>, IAncestorProvider
	{
		public string Expression { get; set; }

		void IVisitorEntity<LocationExpression, ILocationExpression>.Init(ILocationExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IVisitorEntity<LocationExpression, ILocationExpression>.RefEquals(in LocationExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}