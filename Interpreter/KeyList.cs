using System.Collections.Generic;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class KeyList<T> where T : IEntity
	{
		public delegate void ChangeHandler(ChangedAction action, IEntity entity, /**/ImmutableArray<T> list);

		public enum ChangedAction
		{
			Set
		}

		private readonly Dictionary<IEntity, /**/ImmutableArray<T>> _dic = new Dictionary<IEntity, /**/ImmutableArray<T>>();

		public event ChangeHandler Changed;

		public void Set(IEntity entity, /**/ImmutableArray<T> list)
		{
			_dic[entity] = list;

			Changed?.Invoke(ChangedAction.Set, entity, list);
		}

		public bool TryGetValue(IEntity entity, out /**/ImmutableArray<T> list) => _dic.TryGetValue(entity, out list);
	}
}