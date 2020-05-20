using System.Threading;
using System.Threading.Tasks;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public interface ICancelInvokeEvaluator
	{
		ValueTask Cancel(InvokeId invokeId, IExecutionContext executionContext, CancellationToken token);
	}
}