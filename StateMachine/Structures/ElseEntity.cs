namespace TSSArt.StateMachine
{
	public struct ElseEntity : IElse, IVisitorEntity<ElseEntity, IElse>, IAncestorProvider
	{
		internal object? Ancestor;

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => Ancestor;

	#endregion

	#region Interface IVisitorEntity<ElseEntity,IElse>

		void IVisitorEntity<ElseEntity, IElse>.Init(IElse source) => Ancestor = source;

		bool IVisitorEntity<ElseEntity, IElse>.RefEquals(in ElseEntity other) => true;

	#endregion
	}
}