using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptForEachEvaluator : DefaultForEachEvaluator
	{
		public EcmaScriptForEachEvaluator(in ForEach forEach) : base(forEach) { }

		public override async Task Execute(IExecutionContext executionContext, CancellationToken token)
		{
			var engine = executionContext.Engine();

			engine.EnterExecutionContext();

			try
			{
				ItemEvaluator.DeclareLocalVariable(executionContext);
				IndexEvaluator?.DeclareLocalVariable(executionContext);

				await base.Execute(executionContext, token);
			}
			finally
			{
				engine.LeaveExecutionContext();
			}
		}
	}
}