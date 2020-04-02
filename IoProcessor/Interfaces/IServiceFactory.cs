using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IServiceFactory
	{
		Uri  TypeId      { get; }
		Uri? AliasTypeId { get; }

		ValueTask<IService> StartService(Uri? location, InvokeData invokeData, IServiceCommunication serviceCommunication, CancellationToken token);
	}
}