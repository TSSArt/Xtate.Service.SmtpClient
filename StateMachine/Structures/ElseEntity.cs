namespace TSSArt.StateMachine
{
	public struct ElseEntity : IElse, IVisitorEntity<ElseEntity, IElse>, IAncestorProvider
	{
		void IVisitorEntity<ElseEntity, IElse>.Init(IElse source) => Ancestor = source;

		bool IVisitorEntity<ElseEntity, IElse>.RefEquals(in ElseEntity other) => true;

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}