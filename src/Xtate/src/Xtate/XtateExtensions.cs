namespace Xtate
{
	public static class XtateExtensions
	{
		public static StateMachineHostBuilder AddAll(this StateMachineHostBuilder builder) =>
				builder
						.AddEcmaScript()
						.AddHttpClient()
						.AddSmtpClient()
						.SetSerilogLogger();
	}
}