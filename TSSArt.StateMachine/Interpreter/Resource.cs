using System;
using System.Net.Mime;

namespace TSSArt.StateMachine
{
	public class Resource
	{
		public Resource(Uri uri, ContentType contentType, string content)
		{
			Uri = uri;
			ContentType = contentType;
			Content = content;
		}

		public Uri         Uri         { get; }
		public ContentType ContentType { get; }
		public string      Content     { get; }
	}
}