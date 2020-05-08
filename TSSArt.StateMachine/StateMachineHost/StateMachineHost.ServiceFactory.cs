using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class StateMachineHost : IServiceFactory
	{
		private static readonly Uri ServiceFactoryTypeId      = new Uri("http://www.w3.org/TR/scxml/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "scxml", UriKind.Relative);

	#region Interface IServiceFactory

		bool IServiceFactory.CanHandle(Uri type, Uri? source) => FullUriComparer.Instance.Equals(type, ServiceFactoryTypeId) || FullUriComparer.Instance.Equals(type, ServiceFactoryAliasTypeId);

		async ValueTask<IService> IServiceFactory.StartService(Uri? baseUri, InvokeData invokeData, IServiceCommunication serviceCommunication, CancellationToken token)
		{
			var sessionId = SessionId.FromString(invokeData.InvokeId.Value); // using InvokeId as SessionId
			var scxml = invokeData.RawContent ?? invokeData.Content.AsStringOrDefault();
			var context = GetCurrentContext();
			var parameters = invokeData.Parameters;
			var source = invokeData.Source;

			Infrastructure.Assert(scxml != null || source != null);

			var origin = scxml != null ? new StateMachineOrigin(scxml, baseUri) : new StateMachineOrigin(source!, baseUri);

			var errorProcessor = CreateErrorProcessor(sessionId, origin);

			var service = await context.CreateAndAddStateMachine(sessionId, origin, parameters, errorProcessor, token).ConfigureAwait(false);

			await service.StartAsync(token).ConfigureAwait(false);

			CompleteAsync(context, service, sessionId).Forget();

			return service;
		}

	#endregion

		private static async ValueTask CompleteAsync(StateMachineHostContext context, StateMachineController service, SessionId sessionId)
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
	}
}