using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class KeyList<T> where T : IEntity
	{
		public delegate void ChangeHandler(ChangedAction action, IEntity entity, IReadOnlyList<T> list);

		public enum ChangedAction
		{
			Set
		}

		private readonly Dictionary<IEntity, IReadOnlyList<T>> _dic = new Dictionary<IEntity, IReadOnlyList<T>>();

		public event ChangeHandler Changed;

		public void Set(IEntity entity, IReadOnlyList<T> list)
		{
			_dic[entity] = list;

			Changed?.Invoke(ChangedAction.Set, entity, list);
		}

		public bool TryGetValue(IEntity entity, out IReadOnlyList<T> list) => _dic.TryGetValue(entity, out list);
	}
}