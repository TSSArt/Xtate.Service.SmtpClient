using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptContentBodyEvaluator : DefaultContentBodyEvaluator
	{
		public EcmaScriptContentBodyEvaluator(in ContentBody contentBody) : base(in contentBody) { }

		public override ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token)
		{
			Value.
		}
	}
}