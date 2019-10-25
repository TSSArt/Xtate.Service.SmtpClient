using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IExecEvaluator
	{
		Task Execute(IExecutionContext executionContext, CancellationToken token);
	}
}