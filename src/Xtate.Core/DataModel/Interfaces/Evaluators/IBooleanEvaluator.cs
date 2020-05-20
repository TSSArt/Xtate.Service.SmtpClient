using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IBooleanEvaluator : IValueEvaluator
	{
		ValueTask<bool> EvaluateBoolean(IExecutionContext executionContext, CancellationToken token);
	}
}