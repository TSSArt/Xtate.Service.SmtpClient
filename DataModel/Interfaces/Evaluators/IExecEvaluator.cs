using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IExecEvaluator
	{
		ValueTask Execute(IExecutionContext executionContext, CancellationToken token);
	}
}