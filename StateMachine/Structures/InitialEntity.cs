namespace TSSArt.StateMachine
{
	public struct InitialEntity : IInitial, IVisitorEntity<InitialEntity, IInitial>, IAncestorProvider
	{
		public ITransition? Transition { get; set; }

		void IVisitorEntity<InitialEntity, IInitial>.Init(IInitial source)
		{
			Ancestor = source;

			Transition = source.Transition;
		}

		bool IVisitorEntity<InitialEntity, IInitial>.RefEquals(in InitialEntity other) => ReferenceEquals(Transition, other.Transition);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}