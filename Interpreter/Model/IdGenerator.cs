using System;
using System.Globalization;

namespace TSSArt.StateMachine
{
	public static class IdGenerator
	{
		public static string NewSessionId() => Guid.NewGuid().ToString("D");

		public static string NewSendId() => Guid.NewGuid().ToString("D");
		
		public static string NewInvokeId(string stateId) => $"{stateId}.{Guid.NewGuid():D}";

		public static string NewUniqueStateId() => Guid.NewGuid().ToString("D");
	}
}