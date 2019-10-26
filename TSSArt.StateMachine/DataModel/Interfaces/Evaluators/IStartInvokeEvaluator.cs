using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IStartInvokeEvaluator
	{
		ValueTask<string> Start(string stateId, IExecutionContext executionContext, CancellationToken token);
	}
}