using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ICustomActionConsumer
	{
		void SetAction(Func<IExecutionContext, CancellationToken, ValueTask> action);
	}
}