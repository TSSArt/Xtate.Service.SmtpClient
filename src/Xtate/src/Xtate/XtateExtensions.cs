namespace Xtate
{
	public static class XtateExtensions
	{
		public static StateMachineHostBuilder AddAll(this StateMachineHostBuilder builder) =>
				builder
						.AddXPath()
						.AddEcmaScript()
						.AddHttpClient()
						.AddSmtpClient()
						.SetSerilogLogger();
	}
}