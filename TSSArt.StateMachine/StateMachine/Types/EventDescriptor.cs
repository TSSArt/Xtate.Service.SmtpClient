using System;

namespace TSSArt.StateMachine
{
	public sealed class EventDescriptor : IEventDescriptor
	{
		private static readonly char[]        Dot = { '.' };
		private readonly        IIdentifier[] _parts;
		private readonly        string        _val;

		private EventDescriptor(string val)
		{
			_val = val ?? throw new ArgumentNullException(nameof(val));

			if (val.Length == 0)
			{
				throw new ArgumentException(message: "Event cannot be empty", nameof(val));
			}

			var parts = val.Split(Dot, StringSplitOptions.None);
			var length = parts.Length;
			if (length > 0 && parts[length - 1] == "*")
			{
				length --;
			}

			_parts = new IIdentifier[length];

			for (var i = 0; i < _parts.Length; i ++)
			{
				_parts[i] = (Identifier) parts[i];
			}
		}

		public bool IsEventMatch(IEvent @event)
		{
			if (@event == null) throw new ArgumentNullException(nameof(@event));

			if (@event.NameParts.Length < _parts.Length)
			{
				return false;
			}

			for (var i = 0; i < _parts.Length; i ++)
			{
				if (!Equals(@event.NameParts[i], _parts[i]))
				{
					return false;
				}
			}

			return true;
		}

		public static implicit operator EventDescriptor(string val) => new EventDescriptor(val);

		public override string ToString() => _val;
	}
}