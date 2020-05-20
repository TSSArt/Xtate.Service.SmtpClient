using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface ILocationEvaluator
	{
		void DeclareLocalVariable(IExecutionContext executionContext);

		ValueTask SetValue(IObject value, IExecutionContext executionContext, CancellationToken token);

		ValueTask<IObject> GetValue(IExecutionContext executionContext, CancellationToken token);

		string GetName(IExecutionContext executionContext);
	}
}