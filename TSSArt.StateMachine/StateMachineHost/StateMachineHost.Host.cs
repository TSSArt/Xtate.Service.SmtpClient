using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class StateMachineHost : IHost
	{
	#region Interface IHost

		ValueTask<string> IHost.StartStateMachine(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token) => Start(origin, parameters, token);

		ValueTask<DataModelValue> IHost.ExecuteStateMachine(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token) => Execute(origin, parameters, token);

		async ValueTask IHost.DestroyStateMachine(string sessionId, CancellationToken token)
		{
			var context = GetCurrentContext();
			await context.DestroyStateMachine(sessionId).ConfigureAwait(false);
		}

	#endregion

		private async ValueTask<string> Start(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token)
		{
			var context = GetCurrentContext();
			var sessionId = IdGenerator.NewSessionId();
			var errorProcessor = CreateErrorProcessor(sessionId, origin);

			var controller = await context.CreateAndAddStateMachine(sessionId, origin, parameters, errorProcessor, token).ConfigureAwait(false);

			await controller.StartAsync(token).ConfigureAwait(false);

			return sessionId;
		}

		private async ValueTask<DataModelValue> Execute(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token)
		{
			var context = GetCurrentContext();
			var sessionId = IdGenerator.NewSessionId();
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