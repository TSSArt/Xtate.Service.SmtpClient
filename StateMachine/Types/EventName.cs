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

		private static IReadOnlyList<IIdentifier> GetErrorNameParts(IIdentifier type)           => new ReadOnlyCollection<IIdentifier>(new[] { ErrorIdentifier, type });
		public static  IReadOnlyList<IIdentifier> GetDoneStateNameParts(IIdentifier id)         => new ReadOnlyCollection<IIdentifier>(new[] { DoneIdentifier, StateIdentifier, id });
		public static  IReadOnlyList<IIdentifier> GetDoneInvokeNameParts(IIdentifier invokeId)  => new ReadOnlyCollection<IIdentifier>(new[] { DoneIdentifier, InvokeIdentifier, invokeId });
		public static  IReadOnlyList<IIdentifier> GetErrorInvokeNameParts(IIdentifier invokeId) => new ReadOnlyCollection<IIdentifier>(new[] { ErrorIdentifier, InvokeIdentifier, invokeId });

		public static string                     ToName(IReadOnlyList<IIdentifier> nameParts) => string.Join(separator: '.', nameParts);
		public static IReadOnlyList<IIdentifier> ToParts(string name)                         => IdentifierList.Create(name.Split(Dot, StringSplitOptions.None), p => (Identifier) p);
	}
}