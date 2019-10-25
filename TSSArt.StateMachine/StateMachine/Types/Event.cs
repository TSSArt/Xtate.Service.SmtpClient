using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public sealed class Event : IEvent
	{
		private static readonly char[] Dot = { '.' };

		private readonly IReadOnlyList<IIdentifier> _parts;
		private readonly string                     _val;

		private Event(string val)
		{
			_val = val ?? throw new ArgumentNullException(nameof(val));

			if (val == string.Empty)
			{
				throw new ArgumentException(message: "Event cannot be empty", nameof(val));
			}

			_parts = IdentifierList.Create(val.Split(Dot, StringSplitOptions.None), p => (Identifier) p);
		}

		IReadOnlyList<IIdentifier> IEvent.NameParts => _parts;

		DataModelValue IEvent.Data       => DataModelValue.Undefined();
		string IEvent.        InvokeId   => null;
		string IEvent.        SendId     => null;
		Uri IEvent.           Origin     => null;
		Uri IEvent.           OriginType => null;
		EventType IEvent.     Type       => EventType.Internal;

		public static explicit operator Event(string val) => new Event(val);

		public override string ToString() => _val;
	}
}