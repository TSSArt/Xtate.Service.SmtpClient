using System.Threading;
using System.Threading.Tasks;

namespace Xtate
{
	public interface IService
	{
		Task<DataModelValue> Result { get; }
		ValueTask            Send(IEvent evt, CancellationToken token);
		ValueTask            Destroy(CancellationToken token);
	}
}