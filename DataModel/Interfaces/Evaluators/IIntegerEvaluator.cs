using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IIntegerEvaluator : IValueEvaluator
	{
		ValueTask<int> EvaluateInteger(IExecutionContext executionContext, CancellationToken token);
	}
}