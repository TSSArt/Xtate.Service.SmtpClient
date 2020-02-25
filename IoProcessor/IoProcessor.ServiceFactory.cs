using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class IoProcessor : IServiceFactory
	{
		private static readonly Uri ServiceFactoryTypeId      = new Uri("http://www.w3.org/TR/scxml/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "scxml", UriKind.Relative);

		Uri IServiceFactory.TypeId => ServiceFactoryTypeId;

		Uri IServiceFactory.AliasTypeId => ServiceFactoryAliasTypeId;

		async ValueTask<IService> IServiceFactory.StartService(Uri source, string rawContent, DataModelValue content, DataModelValue parameters, 
															   IServiceCommunication serviceCommunication, CancellationToken token)
		{
			var sessionId = IdGenerator.NewSessionId();
			var scxml = rawContent ?? content.AsStringOrDefault();
			var service = await _context.CreateAndAddStateMachine(sessionId, options: null, stateMachine: null, source: source, scxml: scxml, parameters: parameters, token: token).ConfigureAwait(false);

			await service.StartAsync(token).ConfigureAwait(false);

			CompleteAsync();

			async void CompleteAsync()
			{
				await service.Result.ConfigureAwait(false);
				await _context.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}

			return service;
		}
	}
}