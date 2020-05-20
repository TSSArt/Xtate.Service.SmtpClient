using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IExpressionEvaluator
	{
		ValueTask<DataModelValue> Evaluate(IExecutionContext executionContext, CancellationToken token);
	}
}