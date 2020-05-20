using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface ICustomActionExecutor
	{
		ValueTask Execute(IExecutionContext executionContext, CancellationToken token);
	}
}