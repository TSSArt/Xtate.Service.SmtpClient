using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class EntityQueue<T> where T : IEntity
	{
		public delegate void ChangeHandler(ChangedAction action, T entity);

		public enum ChangedAction
		{
			Enqueue,
			Dequeue
		}

		private readonly Queue<T> _queue = new Queue<T>();

		public int Count => _queue.Count;

		public event ChangeHandler Changed;

		public T Dequeue()
		{
			Changed?.Invoke(ChangedAction.Dequeue, entity: default);

			return _queue.Dequeue();
		}

		public void Enqueue(T item)
		{
			_queue.Enqueue(item);

			Changed?.Invoke(ChangedAction.Enqueue, item);
		}
	}
}