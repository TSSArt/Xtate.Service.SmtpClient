using System.Threading;
using System.Threading.Tasks;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public interface IArrayEvaluator : IValueEvaluator
	{
		ValueTask<IObject[]> EvaluateArray(IExecutionContext executionContext, CancellationToken token);
	}
}