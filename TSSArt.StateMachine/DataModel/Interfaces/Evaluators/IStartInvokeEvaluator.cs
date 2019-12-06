using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IStartInvokeEvaluator
	{
		ValueTask<(string InvokeId, string InvokeUniqueId)> Start(string stateId, IExecutionContext executionContext, CancellationToken token);
	}
}