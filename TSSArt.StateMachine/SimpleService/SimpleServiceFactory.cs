using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed class SimpleServiceFactory<TService> : IServiceFactory where TService : SimpleServiceBase, new()
	{
		public static readonly IServiceFactory Instance = new SimpleServiceFactory<TService>();
		private readonly       Uri             _alias;

		private readonly Uri _type;

		private SimpleServiceFactory()
		{
			var serviceAttribute = typeof(TService).GetCustomAttribute<SimpleServiceAttribute>();

			if (serviceAttribute == null)
			{
				throw new InvalidOperationException("ServiceAttribute did not provided for type " + typeof(TService));
			}

			_type = new Uri(serviceAttribute.Type, UriKind.RelativeOrAbsolute);
			_alias = serviceAttribute.Alias != null ? new Uri(serviceAttribute.Alias, UriKind.RelativeOrAbsolute) : null;
		}

		ValueTask<IService> IServiceFactory.StartService(Uri source, string rawContent, DataModelValue content, DataModelValue parameters, IServiceCommunication serviceCommunication, CancellationToken token)
		{
			var service = new TService();

			service.Start(source, rawContent, content, parameters, serviceCommunication);

			return new ValueTask<IService>(service);
		}

		Uri IServiceFactory.TypeId      => _type;
		Uri IServiceFactory.AliasTypeId => _alias;
	}
}