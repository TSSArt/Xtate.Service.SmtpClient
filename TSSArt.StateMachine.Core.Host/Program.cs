using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using TSSArt.StateMachine.Services;

namespace TSSArt.StateMachine.Core.Host
{
	internal static class Program
	{
		[STAThread]
		public static async Task Main()
		{
#if NETCOREAPP3_0
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var handler = new HttpEventProcessorHandler(new Uri("http://localhost:5000/"));
			var httpEventProcessor = handler.CreateEventProcessor();
			var options = new IoProcessorOptions
						  {
								  EventProcessors = new[] { httpEventProcessor },
								  ServiceFactories = new[] { WebBrowserService.GetFactory<CefSharpWebBrowserService>() }
						  };

			await using var ioProcessor = new IoProcessor(options);

			var captchaTask = ioProcessor.Execute(sessionId: "captcha", GetCaptchaStateMachine());

			var webHost = new WebHostBuilder()
						  .Configure(builder => builder.Run(handler.ProcessRequest))
						  .UseKestrel()
						  .Build();

			await webHost.StartAsync().ConfigureAwait(false);

			Application.Run(new MainForm());

			//await Task.Delay(-1).ConfigureAwait(false);
		}

		private static IStateMachine GetCaptchaStateMachine()
		{
			return new StateMachineFluentBuilder(new BuilderFactory())
					.BeginState("idle")
						.AddTransition(eventDescriptor: "show", target: "dialog")
						.AddOnEntry(ctx => ctx.Log("Enter in idle", default, default))
						.AddOnExit(ctx => ctx.Log("Exit from idle", default, default))
					.EndState()
					.BeginState("dialog")
						.AddOnEntry((context, token) => context.Send(new Event("timeout") { DelayMs = 60_000 }, token))
						.AddTransition(eventDescriptor: "close", target: "idle")
						.AddTransition(eventDescriptor: "timeout", target: "idle")
						.AddOnEntry(ctx => ctx.Log("Enter in dialog", default, default))
						.AddOnExit(ctx => ctx.Log("Exit from dialog", default, default))
						.AddOnEntry(ctx =>
									{
										var source = ctx.DataModel["_event"].AsObject()["data"].AsObject()["source"].AsString();
										return ctx.StartInvoke("brw", new Uri("http://tssart.com/scxml/service/browser"), new Uri(source), default, default, default);
									})
						.AddOnExit(ctx => ctx.CancelInvoke("brw", default))
					.EndState()
					.Build();
		}
	}
}