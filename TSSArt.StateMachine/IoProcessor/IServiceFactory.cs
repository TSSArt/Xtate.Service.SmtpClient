using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IServiceFactory
	{
		Uri            TypeId      { get; }
		Uri            AliasTypeId { get; }
		Task<IService> StartService(IService parentService, Uri source, DataModelValue data, CancellationToken token);
	}
}