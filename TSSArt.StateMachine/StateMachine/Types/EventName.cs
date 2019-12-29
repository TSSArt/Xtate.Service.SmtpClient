using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TSSArt.StateMachine
{
	public static class EventName
	{
		private const           char   Dot      = '.';
		private static readonly char[] DotArray = { Dot };

		private static readonly IIdentifier DoneIdentifier   = (Identifier) "done";
		private static readonly IIdentifier StateIdentifier  = (Identifier) "state";
		private static readonly IIdentifier ErrorIdentifier  = (Identifier) "error";
		private static readonly IIdentifier InvokeIdentifier = (Identifier) "invoke";

		public static readonly IReadOnlyList<IIdentifier> ErrorExecution     = GetErrorNameParts((Identifier) "execution");
		public static readonly IReadOnlyList<IIdentifier> ErrorCommunication = GetErrorNameParts((Identifier) "communication");
		public static readonly IReadOnlyList<IIdentifier> ErrorPlatform      = GetErrorNameParts((Identifier) "platform");

		private static IReadOnlyList<IIdentifier> GetErrorNameParts(IIdentifier type) => new ReadOnlyCollection<IIdentifier>(new[] { ErrorIdentifier, type });

		internal static IReadOnlyList<IIdentifier> GetDoneStateNameParts(IIdentifier id)    => new ReadOnlyCollection<IIdentifier>(new[] { DoneIdentifier, StateIdentifier, id });
		internal static IReadOnlyList<IIdentifier> GetDoneInvokeNameParts(string invokeId)  => GetInvokeNameParts(DoneIdentifier, invokeId);
		internal static IReadOnlyList<IIdentifier> GetErrorInvokeNameParts(string invokeId) => GetInvokeNameParts(ErrorIdentifier, invokeId);

		private static IReadOnlyList<IIdentifier> GetInvokeNameParts(IIdentifier identifier, string invokeId)
		{
			if (invokeId == null) throw new ArgumentNullException(nameof(invokeId));

			IIdentifier[] parts;

			if (invokeId.IndexOf(Dot) < 0)
			{
				parts = new IIdentifier[3];
				parts[2] = (Identifier) invokeId;
			}
			else
			{
				var invokeIdParts = invokeId.Split(DotArray, StringSplitOptions.None);
				parts = new IIdentifier[invokeIdParts.Length + 2];
				for (var i = 0; i < invokeIdParts.Length; i ++)
				{
					parts[i + 2] = (Identifier) invokeIdParts[i];
				}
			}

			parts[0] = identifier;
			parts[1] = InvokeIdentifier;

			return new ReadOnlyCollection<IIdentifier>(parts);
		}

		public static string ToName(IReadOnlyList<IIdentifier> nameParts)
		{
			if (nameParts == null) throw new ArgumentNullException(nameof(nameParts));

			return string.Join(separator: ".", nameParts.Select(p => p.Base<IIdentifier>().ToString()));
		}

		public static IReadOnlyList<IIdentifier> ToParts(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			return IdentifierList.Create(name.Split(DotArray, StringSplitOptions.None), Identifier.FromString);
		}
	}
}