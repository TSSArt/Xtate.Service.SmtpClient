using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class ForEachBuilder : IForEachBuilder
	{
		private readonly List<IExecutableEntity> _actions = new List<IExecutableEntity>();
		private          IValueExpression        _array;
		private          ILocationExpression     _index;
		private          ILocationExpression     _item;

		public IForEach Build()
		{
			if (_array == null)
			{
				throw new InvalidOperationException(message: "Array property required for ForEach element");
			}

			if (_item == null)
			{
				throw new InvalidOperationException(message: "Condition property required for ForEach element");
			}

			return new ForEach { Array = _array, Item = _item, Index = _index, Action = _actions };
		}

		public void SetArray(IValueExpression array) => _array = array ?? throw new ArgumentNullException(nameof(array));

		public void SetItem(ILocationExpression item)
		{
			_item = item ?? throw new ArgumentNullException(nameof(item));
		}

		public void SetIndex(ILocationExpression index)
		{
			_index = index ?? throw new ArgumentNullException(nameof(index));
		}

		public void AddAction(IExecutableEntity action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			_actions.Add(action);
		}
	}
}