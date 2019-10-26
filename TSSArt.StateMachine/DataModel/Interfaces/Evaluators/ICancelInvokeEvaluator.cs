using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ICancelInvokeEvaluator
	{
		ValueTask Cancel(string invokeId, IExecutionContext executionContext, CancellationToken token);
	}
}