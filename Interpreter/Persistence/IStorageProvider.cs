using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IStorageProvider
	{
		ValueTask<ITransactionalStorage> GetTransactionalStorage(string sessionId, string name, CancellationToken token);
		ValueTask                        RemoveTransactionalStorage(string sessionId, string name, CancellationToken token);
		ValueTask                        RemoveAllTransactionalStorage(string sessionId, CancellationToken token);
	}
}