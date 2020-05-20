namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#WebBrowser", Alias = "browser")]
	public abstract class WebBrowserService : SimpleServiceBase
	{
		public static IServiceFactory GetFactory<T>() where T : WebBrowserService, new() => SimpleServiceFactory<T>.Instance;
	}
}