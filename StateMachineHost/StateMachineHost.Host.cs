using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class StateMachineHost : IHost
	{
	#region Interface IHost

		ValueTask<SessionId> IHost.StartStateMachine(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token) => Start(origin, parameters, token);

		ValueTask<DataModelValue> IHost.ExecuteStateMachine(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token) => ExecuteAsync(origin, parameters, token);

		ValueTask IHost.DestroyStateMachine(SessionId sessionId, CancellationToken token) => GetCurrentContext().DestroyStateMachine(sessionId);

	#endregion

		private async ValueTask<SessionId> Start(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token)
		{
			var context = GetCurrentContext();
			var sessionId = SessionId.New();
			var errorProcessor = CreateErrorProcessor(sessionId, origin);

			var controller = await context.CreateAndAddStateMachine(sessionId, origin, parameters, errorProcessor, token).ConfigureAwait(false);

			await controller.StartAsync(token).ConfigureAwait(false);

			return sessionId;
		}

		private async ValueTask<DataModelValue> ExecuteAsync(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token)
		{
			var context = GetCurrentContext();
			var sessionId = SessionId.New();
			var errorProcessor = CreateErrorProcessor(sessionId, origin);

			var controller = await context.CreateAndAddStateMachine(sessionId, origin, parameters, errorProcessor, token).ConfigureAwait(false);

			try
			{
				return await controller.ExecuteAsync().ConfigureAwait(false);
			}
			finally
			{
				await context.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}
		}
	}
}