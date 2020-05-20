using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IHost
	{
		ValueTask StartStateMachineAsync(SessionId sessionId, StateMachineOrigin origin, DataModelValue parameters, CancellationToken token);

		ValueTask<DataModelValue> ExecuteStateMachineAsync(SessionId sessionId, StateMachineOrigin origin, DataModelValue parameters, CancellationToken token);

		void DestroyStateMachine(SessionId sessionId);
	}
}