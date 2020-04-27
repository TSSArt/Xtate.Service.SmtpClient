using System;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine
{
	public static class CefSharpWebBrowserExtensions
	{
		public static StateMachineHostBuilder AddCefSharpWebBrowser(this StateMachineHostBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddServiceFactory(WebBrowserService.GetFactory<CefSharpWebBrowserService>());

			return builder;
		}
	}
}