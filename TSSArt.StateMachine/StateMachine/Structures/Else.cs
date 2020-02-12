namespace TSSArt.StateMachine
{
	public struct Else : IElse, IVisitorEntity<Else, IElse>, IAncestorProvider
	{
		void IVisitorEntity<Else, IElse>.Init(IElse source)
		{
			Ancestor = source;
		}

		bool IVisitorEntity<Else, IElse>.RefEquals(in Else other) => true;

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}