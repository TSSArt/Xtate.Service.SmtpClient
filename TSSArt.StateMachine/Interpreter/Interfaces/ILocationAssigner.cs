using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ILocationAssigner
	{
		ValueTask Assign(IExecutionContext executionContext, DataModelValue value, CancellationToken token);
	}
}