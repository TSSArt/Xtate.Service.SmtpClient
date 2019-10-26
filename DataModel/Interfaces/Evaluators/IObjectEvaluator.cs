using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IObjectEvaluator : IValueEvaluator
	{
		ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token);
	}
}