using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IServiceFactory
	{
		Uri TypeId      { get; }
		Uri AliasTypeId { get; }

		ValueTask<IService> StartService(Uri source, DataModelValue content, DataModelValue parameters, CancellationToken token);
	}
}