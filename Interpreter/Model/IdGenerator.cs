using System;

namespace TSSArt.StateMachine
{
	internal static class IdGenerator
	{
		private static string NewGuidString() => Guid.NewGuid().ToString(format: "D", provider: default);

		public static string NewSessionId() => NewGuidString();

		public static string NewSendId() => NewGuidString();

		public static string NewInvokeId(string stateId) => stateId + @"." + NewGuidString();

		public static string NewInvokeUniqueId() => NewGuidString();

		public static string NewUniqueStateId() => NewGuidString();
	}
}