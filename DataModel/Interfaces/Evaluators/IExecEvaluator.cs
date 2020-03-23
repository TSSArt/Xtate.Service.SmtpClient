using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IExecEvaluator
	{
		ValueTask Execute(IExecutionContext executionContext, CancellationToken token);
	}
}