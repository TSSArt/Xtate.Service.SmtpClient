using System;
using System.ComponentModel;

namespace TSSArt.StateMachine
{
	public class HistoryBuilder : IHistoryBuilder
	{
		private IIdentifier _id;
		private ITransition _transition;
		private HistoryType _type;

		public IHistory Build()
		{
			if (_transition == null)
			{
				throw new InvalidOperationException(message: "Transition must be present in History element");
			}

			return new History { Id = _id, Type = _type, Transition = _transition };
		}

		public void SetId(IIdentifier id) => _id = id ?? throw new ArgumentNullException(nameof(id));

		public void SetType(HistoryType type)
		{
			if (type < HistoryType.Shallow || type > HistoryType.Deep) throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(HistoryType));

			_type = type;
		}

		public void SetTransition(ITransition transition) => _transition = transition ?? throw new ArgumentNullException(nameof(transition));
	}
}