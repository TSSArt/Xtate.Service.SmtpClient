using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

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
			var options = new IoProcessorOptions { EventProcessors = new[] { httpEventProcessor } };
			await using var ioProcessor = new IoProcessor(options);

			var webHost = new WebHostBuilder().Configure(builder => builder.Run(handler.ProcessRequest)).UseKestrel().Build();

			await webHost.StartAsync();

			await Task.Delay(-1);
		}
	}
}