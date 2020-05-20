using System;
using System.Net.Mime;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class Resource
	{
		public Resource(Uri uri, ContentType? contentType, string content)
		{
			Uri = uri;
			ContentType = contentType;
			Content = content;
		}

		public Uri          Uri         { get; }
		public ContentType? ContentType { get; }
		public string       Content     { get; }
	}
}