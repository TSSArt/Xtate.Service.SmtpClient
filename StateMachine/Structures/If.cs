using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct If : IIf, IEntity<If, IIf>, IAncestorProvider
	{
		public IReadOnlyList<IExecutableEntity> Action;
		public IConditionExpression             Condition;

		IReadOnlyList<IExecutableEntity> IIf.Action => Action;

		IConditionExpression IIf.Condition => Condition;

		void IEntity<If, IIf>.Init(IIf source)
		{
			Ancestor = source;
			Action = source.Action;
			Condition = source.Condition;
		}

		bool IEntity<If, IIf>.RefEquals(in If other) =>
				ReferenceEquals(Action, other.Action) &&
				ReferenceEquals(Condition, other.Condition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}