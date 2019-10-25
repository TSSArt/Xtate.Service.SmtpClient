namespace TSSArt.StateMachine
{
	public struct ElseIf : IElseIf, IEntity<ElseIf, IElseIf>, IAncestorProvider
	{
		public IConditionExpression Condition;

		IConditionExpression IElseIf.Condition => Condition;

		void IEntity<ElseIf, IElseIf>.Init(IElseIf source)
		{
			Ancestor = source;
			Condition = source.Condition;
		}

		bool IEntity<ElseIf, IElseIf>.RefEquals(in ElseIf other) => ReferenceEquals(Condition, other.Condition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}