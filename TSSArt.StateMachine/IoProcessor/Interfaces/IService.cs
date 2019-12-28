using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public struct ServiceResult
	{
		public string         DoneEventSuffix { get; set; }
		public DataModelValue DoneEventData   { get; set; }
	}

	public interface IService
	{
		ValueTask<ServiceResult> GetResult();
		ValueTask                Send(IEvent @event, CancellationToken token);
		ValueTask                Destroy(CancellationToken token);
	}
}