using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TSSArt.StateMachine
{
	public static class EventName
	{
		private static readonly char[] Dot = { '.' };

		private static readonly IIdentifier DoneIdentifier   = (Identifier) "done";
		private static readonly IIdentifier StateIdentifier  = (Identifier) "state";
		private static readonly IIdentifier ErrorIdentifier  = (Identifier) "error";
		private static readonly IIdentifier InvokeIdentifier = (Identifier) "invoke";

		public static readonly IReadOnlyList<IIdentifier> ErrorExecution     = GetErrorNameParts((Identifier) "execution");
		public static readonly IReadOnlyList<IIdentifier> ErrorCommunication = GetErrorNameParts((Identifier) "communication");
		public static readonly IReadOnlyList<IIdentifier> ErrorPlatform      = GetErrorNameParts((Identifier) "platform");

		private static  IReadOnlyList<IIdentifier> GetErrorNameParts(IIdentifier type)          => new ReadOnlyCollection<IIdentifier>(new[] { ErrorIdentifier, type });
		internal static IReadOnlyList<IIdentifier> GetDoneStateNameParts(IIdentifier id)        => new ReadOnlyCollection<IIdentifier>(new[] { DoneIdentifier, StateIdentifier, id });
		internal static IReadOnlyList<IIdentifier> GetDoneInvokeNameParts(IIdentifier invokeId) => new ReadOnlyCollection<IIdentifier>(new[] { DoneIdentifier, InvokeIdentifier, invokeId });
		internal static IReadOnlyList<IIdentifier> GetErrorInvokeNameParts(IIdentifier invokeId) => new ReadOnlyCollection<IIdentifier>(new[] { ErrorIdentifier, InvokeIdentifier, invokeId });

		public static string ToName(IReadOnlyList<IIdentifier> nameParts)
		{
			if (nameParts == null) throw new ArgumentNullException(nameof(nameParts));

			return string.Join(separator: '.', nameParts);
		}

		public static IReadOnlyList<IIdentifier> ToParts(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			return IdentifierList.Create(name.Split(Dot, StringSplitOptions.None), p => (Identifier) p);
		}
	}
}