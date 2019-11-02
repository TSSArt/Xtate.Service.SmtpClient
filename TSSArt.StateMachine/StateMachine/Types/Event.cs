using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Event : IOutgoingEvent, IAncestorProvider
	{
		private static readonly Uri    InternalTarget = new Uri(uriString: "_internal", UriKind.Relative);
		private static readonly char[] Dot            = { '.' };

		public DataModelValue             Data;
		public int                        DelayMs;
		public IReadOnlyList<IIdentifier> NameParts;
		public string                     SendId;
		public Uri                        Target;
		public Uri                        Type;

		public Event(string val) : this()
		{
			if (string.IsNullOrEmpty(val)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(val));

			NameParts = IdentifierList.Create(val.Split(Dot, StringSplitOptions.None), p => (Identifier) p);
			Target = InternalTarget;
		}

		DataModelValue IOutgoingEvent.Data => Data;

		int IOutgoingEvent.DelayMs => DelayMs;

		IReadOnlyList<IIdentifier> IOutgoingEvent.NameParts => NameParts;

		string IOutgoingEvent.SendId => SendId;

		Uri IOutgoingEvent.Target => Target;

		Uri IOutgoingEvent.Type => Type;

		public static explicit operator Event(string val) => new Event(val);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}