using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Event : IOutgoingEvent, IAncestorProvider
	{
		public static readonly Uri InternalTarget = new Uri(uriString: "_internal", UriKind.Relative);
		public static readonly Uri ParentTarget   = new Uri(uriString: "_parent", UriKind.Relative);

		public DataModelValue             Data      { get; set; }
		public int                        DelayMs   { get; set; }
		public IReadOnlyList<IIdentifier> NameParts { get; set; }
		public string                     SendId    { get; set; }
		public Uri                        Target    { get; set; }
		public Uri                        Type      { get; set; }

		public Event(string val) : this()
		{
			if (!string.IsNullOrEmpty(val))
			{
				NameParts = EventName.ToParts(val);
			}
		}

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}