using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IStringEvaluator : IValueEvaluator
	{
		ValueTask<string> EvaluateString(IExecutionContext executionContext, CancellationToken token);
	}
}