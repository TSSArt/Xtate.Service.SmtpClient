using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IObjectEvaluator : IValueEvaluator
	{
		ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token);
	}
}