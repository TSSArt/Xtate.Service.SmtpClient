using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IStorageProvider
	{
		Task<ITransactionalStorage> GetTransactionalStorage(string sessionId, string name, CancellationToken token);
		Task                        RemoveTransactionalStorage(string sessionId, string name, CancellationToken token);
		Task                        RemoveAllTransactionalStorage(string sessionId, CancellationToken token);
	}
}