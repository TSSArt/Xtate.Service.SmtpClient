using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IArrayEvaluator : IValueEvaluator
	{
		ValueTask<IObject[]> EvaluateArray(IExecutionContext executionContext, CancellationToken token);
	}
}