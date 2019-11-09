using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Event : IOutgoingEvent, IAncestorProvider
	{
		private static readonly Uri InternalTarget = new Uri(uriString: "_internal", UriKind.Relative);

		public DataModelValue             Data      { get; set; }
		public int                        DelayMs   { get; set; }
		public IReadOnlyList<IIdentifier> NameParts { get; set; }
		public string                     SendId    { get; set; }
		public Uri                        Target    { get; set; }
		public Uri                        Type      { get; set; }

		public Event(string val) : this()
		{
			if (string.IsNullOrEmpty(val)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(val));

			NameParts = EventName.ToParts(val);
			Target = InternalTarget;
		}

		public static explicit operator Event(string val) => new Event(val);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}