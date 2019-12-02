using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public abstract class CustomActionBase
	{
		public abstract ValueTask Action(IExecutionContext context, CancellationToken token);
	}
}