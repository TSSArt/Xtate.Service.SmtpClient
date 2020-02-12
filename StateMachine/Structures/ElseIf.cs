namespace TSSArt.StateMachine
{
	public struct ElseIf : IElseIf, IVisitorEntity<ElseIf, IElseIf>, IAncestorProvider
	{
		public IConditionExpression Condition { get; set; }

		void IVisitorEntity<ElseIf, IElseIf>.Init(IElseIf source)
		{
			Ancestor = source;
			Condition = source.Condition;
		}

		bool IVisitorEntity<ElseIf, IElseIf>.RefEquals(in ElseIf other) => ReferenceEquals(Condition, other.Condition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}