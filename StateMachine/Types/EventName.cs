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

		internal static IReadOnlyList<IIdentifier> GetDoneStateNameParts(IIdentifier id) => new ReadOnlyCollection<IIdentifier>(new[] { DoneIdentifier, StateIdentifier, id });

		internal static IReadOnlyList<IIdentifier> GetDoneInvokeNameParts(string invokeId, string suffix)
		{
			if (invokeId == null) throw new ArgumentNullException(nameof(invokeId));

			var invokeIdPartCount = GetCount(invokeId);
			var suffixPartCount = GetCount(suffix);
			var parts = new IIdentifier[2 + invokeIdPartCount + suffixPartCount];

			parts[0] = DoneIdentifier;
			parts[1] = InvokeIdentifier;

			SetParts(parts.AsSpan(start: 2, invokeIdPartCount), invokeId);
			SetParts(parts.AsSpan(2 + invokeIdPartCount, suffixPartCount), suffix);

			return new ReadOnlyCollection<IIdentifier>(parts);

			static void SetParts(Span<IIdentifier> span, string id)
			{
				if (id == null)
				{
					return;
				}

				var pos = 0;
				int pos2;
				var index = 0;

				while ((pos2 = id.IndexOf(Dot, pos)) >= 0)
				{
					span[index ++] = (Identifier) id.Substring(pos, pos2 - pos);

					pos = pos2 + 1;
				}

				span[index] = (Identifier)id.Substring(pos);
			}

			static int GetCount(string id)
			{
				if (id == null)
				{
					return 0;
				}

				var count = 1;
				var pos = 0;

				while ((pos = id.IndexOf(Dot, pos) + 1) > 0)
				{
					count++;
				}

				return count;
			}
		}

		public static string ToName(IReadOnlyList<IIdentifier> nameParts)
		{
			if (nameParts == null) throw new ArgumentNullException(nameof(nameParts));

			return string.Join(separator: ".", nameParts.Select(p => p.Base<IIdentifier>().ToString()));
		}

		public static IReadOnlyList<IIdentifier> ToParts(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			return IdentifierList.Create(name.Split(DotArray, StringSplitOptions.RemoveEmptyEntries), Identifier.FromString);
		}
	}
}