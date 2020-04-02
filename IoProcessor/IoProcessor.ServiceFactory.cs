using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class IoProcessor : IServiceFactory
	{
		private static readonly Uri ServiceFactoryTypeId      = new Uri("http://www.w3.org/TR/scxml/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "scxml", UriKind.Relative);

	#region Interface IServiceFactory

		async ValueTask<IService> IServiceFactory.StartService(Uri? location, InvokeData invokeData, IServiceCommunication serviceCommunication, CancellationToken token)
		{
			var sessionId = invokeData.InvokeId;
			var scxml = invokeData.RawContent ?? invokeData.Content.AsStringOrDefault();
			var context = GetCurrentContext();
			var source = invokeData.Source;

			if (scxml == null && source?.IsAbsoluteUri == false && location?.IsAbsoluteUri == true)
			{
				source = new Uri(location, source);
			}

			var errorProcessor = CreateErrorProcessor(sessionId, stateMachine: null, source, scxml);

			var service = await context.CreateAndAddStateMachine(sessionId, options: null, stateMachine: null, source, scxml, invokeData.Parameters, errorProcessor, token).ConfigureAwait(false);

			await service.StartAsync(token).ConfigureAwait(false);

			CompleteAsync().Forget();

			async ValueTask CompleteAsync()
			{
				try
				{
					await service.Result.ConfigureAwait(false);
				}
				finally
				{
					await context.DestroyStateMachine(sessionId).ConfigureAwait(false);
				}
			}

			return service;
		}

		Uri IServiceFactory.TypeId => ServiceFactoryTypeId;

		Uri IServiceFactory.AliasTypeId => ServiceFactoryAliasTypeId;

	#endregion
	}
}