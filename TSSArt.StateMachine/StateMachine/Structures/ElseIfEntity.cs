namespace TSSArt.StateMachine
{
	public struct ElseIfEntity : IElseIf, IVisitorEntity<ElseIfEntity, IElseIf>, IAncestorProvider
	{
		public IConditionExpression? Condition { get; set; }

		void IVisitorEntity<ElseIfEntity, IElseIf>.Init(IElseIf source)
		{
			Ancestor = source;
			Condition = source.Condition;
		}

		bool IVisitorEntity<ElseIfEntity, IElseIf>.RefEquals(in ElseIfEntity other) => ReferenceEquals(Condition, other.Condition);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}