using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IStartInvokeEvaluator
	{
		ValueTask<InvokeId> Start(IIdentifier stateId, IExecutionContext executionContext, CancellationToken token);
	}
}