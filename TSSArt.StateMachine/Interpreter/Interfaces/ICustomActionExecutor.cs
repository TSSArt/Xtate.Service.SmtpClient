using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ICustomActionExecutor
	{
		ValueTask Execute(IExecutionContext executionContext, CancellationToken token);
	}
}