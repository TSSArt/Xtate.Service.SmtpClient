using System;

namespace TSSArt.StateMachine
{
	public class EventTarget
	{
		public static readonly Uri InternalTarget   = new Uri(uriString: "_internal", UriKind.Relative);
		public static readonly Uri IoInternalTarget = new Uri(uriString: "#_internal", UriKind.Relative);

		public static bool IsInternalTarget(Uri target) => target == InternalTarget || target == IoInternalTarget;
	}
}