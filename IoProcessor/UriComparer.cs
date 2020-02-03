using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal class UriComparer : IEqualityComparer<Uri>
	{
		public static readonly IEqualityComparer<Uri> Instance = new UriComparer();

		public bool Equals(Uri x, Uri y) => x == y && GetSafeFragment(x) == GetSafeFragment(y);

		public int GetHashCode(Uri uri) => (uri.GetHashCode() * 397) ^ GetSafeFragment(uri).GetHashCode();

		private static string GetSafeFragment(Uri uri) => uri != null && uri.IsAbsoluteUri ? uri.Fragment : string.Empty;
	}
}