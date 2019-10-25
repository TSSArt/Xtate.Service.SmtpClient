using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface ITransactionalStorage : IStorage, IAsyncDisposable
	{
		Task CheckPoint(int level, CancellationToken token);
		Task Shrink(CancellationToken token);
	}
}