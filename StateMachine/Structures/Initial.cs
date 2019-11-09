namespace TSSArt.StateMachine
{
	public struct Initial : IInitial, IEntity<Initial, IInitial>, IAncestorProvider
	{
		public ITransition Transition { get; set; }

		void IEntity<Initial, IInitial>.Init(IInitial source)
		{
			Ancestor = source;

			Transition = source.Transition;
		}

		bool IEntity<Initial, IInitial>.RefEquals(in Initial other) => ReferenceEquals(Transition, other.Transition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}