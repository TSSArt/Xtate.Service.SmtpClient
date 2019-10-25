using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IArrayEvaluator : IValueEvaluator
	{
		ValueTask<IObject[]> EvaluateArray(IExecutionContext executionContext, CancellationToken token);
	}
}