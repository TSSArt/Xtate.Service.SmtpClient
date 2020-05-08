using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IHost
	{
		ValueTask<SessionId> StartStateMachine(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token);

		ValueTask<DataModelValue> ExecuteStateMachine(StateMachineOrigin origin, DataModelValue parameters, CancellationToken token);

		ValueTask DestroyStateMachine(SessionId sessionId, CancellationToken token);
	}
}