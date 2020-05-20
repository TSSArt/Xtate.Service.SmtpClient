using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IServiceFactory
	{
		bool CanHandle(Uri type, Uri? source);

		ValueTask<IService> StartService(Uri? baseUri, InvokeData invokeData, IServiceCommunication serviceCommunication, CancellationToken token);
	}
}