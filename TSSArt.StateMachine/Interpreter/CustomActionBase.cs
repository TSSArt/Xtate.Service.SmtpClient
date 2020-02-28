using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class CustomActionBase : ICustomActionExecutor
	{
		internal static readonly ICustomActionExecutor NoExecutorInstance = new CustomActionBase();

		public virtual ValueTask Execute(IExecutionContext context, CancellationToken token)
		{
			throw new NotSupportedException("Custom action does not supported");
		}
	}
}