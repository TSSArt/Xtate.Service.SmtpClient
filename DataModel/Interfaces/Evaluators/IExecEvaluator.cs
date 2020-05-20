using System.Threading;
using System.Threading.Tasks;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public interface IExecEvaluator
	{
		ValueTask Execute(IExecutionContext executionContext, CancellationToken token);
	}
}