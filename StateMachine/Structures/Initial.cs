namespace TSSArt.StateMachine
{
	public struct Initial : IInitial, IVisitorEntity<Initial, IInitial>, IAncestorProvider
	{
		public ITransition Transition { get; set; }

		void IVisitorEntity<Initial, IInitial>.Init(IInitial source)
		{
			Ancestor = source;

			Transition = source.Transition;
		}

		bool IVisitorEntity<Initial, IInitial>.RefEquals(in Initial other) => ReferenceEquals(Transition, other.Transition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}