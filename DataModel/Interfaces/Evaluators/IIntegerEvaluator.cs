using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IIntegerEvaluator : IValueEvaluator
	{
		ValueTask<int> EvaluateInteger(IExecutionContext executionContext, CancellationToken token);
	}
}