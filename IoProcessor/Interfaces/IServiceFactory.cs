using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IServiceFactory
	{
		Uri TypeId      { get; }
		Uri AliasTypeId { get; }

		ValueTask<IService> StartService(Uri source, string rawContent, DataModelValue content, DataModelValue parameters, IServiceCommunication serviceCommunication, CancellationToken token);
	}
}