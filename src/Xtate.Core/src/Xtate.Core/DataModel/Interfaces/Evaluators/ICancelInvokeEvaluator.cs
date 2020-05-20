using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface ICancelInvokeEvaluator
	{
		ValueTask Cancel(InvokeId invokeId, IExecutionContext executionContext, CancellationToken token);
	}
}