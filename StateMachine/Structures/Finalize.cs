using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Finalize : IFinalize, IEntity<Finalize, IFinalize>, IAncestorProvider
	{
		public IReadOnlyList<IExecutableEntity> Action { get; set; }

		void IEntity<Finalize, IFinalize>.Init(IFinalize source)
		{
			Ancestor = source;
			Action = source.Action;
		}

		bool IEntity<Finalize, IFinalize>.RefEquals(in Finalize other) => ReferenceEquals(Action, other.Action);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}