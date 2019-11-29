using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ICustomActionProvider
	{
		bool CanHandle(string ns, string name);

		Func<IExecutionContext, CancellationToken, ValueTask> GetAction(string xml);
	}
}