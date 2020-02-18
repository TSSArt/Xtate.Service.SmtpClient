﻿using System;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	internal sealed class DefaultResourceLoader : IResourceLoader
	{
		public static readonly IResourceLoader Instance = new DefaultResourceLoader();

		public async ValueTask<Resource> Request(Uri uri, CancellationToken token)
		{
			using var client = new HttpClient();
			using var responseMessage = await client.GetAsync(uri, token).ConfigureAwait(false);

			responseMessage.EnsureSuccessStatusCode();

			var contentType = new ContentType(responseMessage.Content.Headers.ContentType.ToString());
			var content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

			return new Resource(uri, contentType, content);
		}

		public ValueTask<XmlReader> RequestXmlReader(Uri uri, XmlReaderSettings readerSettings = null, XmlParserContext parserContext = null, CancellationToken token = default) =>
				new ValueTask<XmlReader>(XmlReader.Create(uri.ToString(), readerSettings, parserContext));
	}
}