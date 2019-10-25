using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IBooleanEvaluator : IValueEvaluator
	{
		ValueTask<bool> EvaluateBoolean(IExecutionContext executionContext, CancellationToken token);
	}
}