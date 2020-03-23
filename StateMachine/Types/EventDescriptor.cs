using System;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public sealed class EventDescriptor : IEventDescriptor, IAncestorProvider
	{
		private static readonly char[] Dot = { '.' };

		private readonly IIdentifier[] _parts;
		private readonly string        _val;

		private EventDescriptor(string val)
		{
			if (string.IsNullOrEmpty(val)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(val));

			_val = val;

			var parts = val.Split(Dot, StringSplitOptions.None);
			var length = parts.Length;
			if (length > 0 && parts[length - 1] == @"*")
			{
				length --;
			}

			_parts = new IIdentifier[length];

			for (var i = 0; i < _parts.Length; i ++)
			{
				_parts[i] = (Identifier) parts[i];
			}
		}

	#region Interface IAncestorProvider

		object IAncestorProvider.Ancestor => _val;

	#endregion

	#region Interface IEventDescriptor

		public bool IsEventMatch(IEvent evt)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			if (evt.NameParts.Length < _parts.Length)
			{
				return false;
			}

			for (var i = 0; i < _parts.Length; i ++)
			{
				if (!Equals(evt.NameParts[i], _parts[i]))
				{
					return false;
				}
			}

			return true;
		}

	#endregion

		public static explicit operator EventDescriptor(string val) => new EventDescriptor(val);

		public static EventDescriptor FromString(string val) => new EventDescriptor(val);
	}
}