using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IStorageProvider
	{
		ValueTask<ITransactionalStorage> GetTransactionalStorage(string name, CancellationToken token);
		ValueTask                        RemoveTransactionalStorage(string name, CancellationToken token);
		ValueTask                        RemoveAllTransactionalStorage(CancellationToken token);
	}
}