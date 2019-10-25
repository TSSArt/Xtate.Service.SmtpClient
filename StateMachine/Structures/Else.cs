namespace TSSArt.StateMachine
{
	public struct Else : IElse, IEntity<Else, IElse>, IAncestorProvider
	{
		void IEntity<Else, IElse>.Init(IElse source)
		{
			Ancestor = source;
		}

		bool IEntity<Else, IElse>.RefEquals(in Else other) => true;

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}