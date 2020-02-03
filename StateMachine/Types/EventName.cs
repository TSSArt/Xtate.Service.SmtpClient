using System;
using System.Collections./**/Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace TSSArt.StateMachine
{
	public static class EventName
	{
		private const char Dot = '.';

		private static readonly IIdentifier DoneIdentifier   = (Identifier) "done";
		private static readonly IIdentifier StateIdentifier  = (Identifier) "state";
		private static readonly IIdentifier ErrorIdentifier  = (Identifier) "error";
		private static readonly IIdentifier InvokeIdentifier = (Identifier) "invoke";

		public static readonly /**/ImmutableArray<IIdentifier> ErrorExecution     = /**/ImmutableArray.Create(ErrorIdentifier, (Identifier) "execution");
		public static readonly /**/ImmutableArray<IIdentifier> ErrorCommunication = /**/ImmutableArray.Create(ErrorIdentifier, (Identifier) "communication");
		public static readonly /**/ImmutableArray<IIdentifier> ErrorPlatform      = /**/ImmutableArray.Create(ErrorIdentifier, (Identifier) "platform");

		internal static /**/ImmutableArray<IIdentifier> GetDoneStateNameParts(IIdentifier id) => GetNameParts(DoneIdentifier, StateIdentifier, id.Base<IIdentifier>().ToString());

		internal static /**/ImmutableArray<IIdentifier> GetDoneInvokeNameParts(string invokeId) => GetNameParts(DoneIdentifier, InvokeIdentifier, invokeId);

		private static /**/ImmutableArray<IIdentifier> GetNameParts(IIdentifier id1, IIdentifier id2, string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));

			var invokeIdPartCount = GetCount(name);
			var parts = new IIdentifier[2 + invokeIdPartCount];

			parts[0] = id1;
			parts[1] = id2;

			SetParts(parts.AsSpan(start: 2, invokeIdPartCount), name);

			return /**/ImmutableArray.Create(parts);
		}

		private static void SetParts(Span<IIdentifier> span, string id)
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

			span[index] = (Identifier) id.Substring(pos);
		}

		private static int GetCount(string id)
		{
			if (id == null)
			{
				return 0;
			}

			var count = 1;
			var pos = 0;

			while ((pos = id.IndexOf(Dot, pos) + 1) > 0)
			{
				count ++;
			}

			return count;
		}

		public static string ToName(/**/ImmutableArray<IIdentifier> nameParts)
		{
			if (nameParts == null) throw new ArgumentNullException(nameof(nameParts));

			return string.Join(separator: ".", nameParts.Select(p => p.Base<IIdentifier>().ToString()));
		}

		public static /**/ImmutableArray<IIdentifier> ToParts(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			if (name == null) throw new ArgumentNullException(nameof(name));

			var parts = new IIdentifier[GetCount(name)];

			SetParts(parts, name);

			return /**/ImmutableArray.Create(parts);
		}
	}
}