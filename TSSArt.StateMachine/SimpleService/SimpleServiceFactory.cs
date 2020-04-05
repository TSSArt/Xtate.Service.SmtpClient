using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed class SimpleServiceFactory<TService> : IServiceFactory where TService : SimpleServiceBase, new()
	{
		public static readonly IServiceFactory Instance = new SimpleServiceFactory<TService>();

		private readonly Uri? _alias;
		private readonly Uri  _type;

		private SimpleServiceFactory()
		{
			var serviceAttribute = typeof(TService).GetCustomAttribute<SimpleServiceAttribute>();

			if (serviceAttribute == null)
			{
				throw new StateMachineInfrastructureException(Res.Format(Resources.Exception_ServiceAttribute_did_not_provided_for_type, typeof(TService)));
			}

			_type = new Uri(serviceAttribute.Type, UriKind.RelativeOrAbsolute);
			_alias = serviceAttribute.Alias != null ? new Uri(serviceAttribute.Alias, UriKind.RelativeOrAbsolute) : null;
		}

	#region Interface IServiceFactory

		bool IServiceFactory.CanHandle(Uri type, Uri? source) => FullUriComparer.Instance.Equals(type, _type) || FullUriComparer.Instance.Equals(type, _alias);

		ValueTask<IService> IServiceFactory.StartService(Uri? location, InvokeData invokeData, IServiceCommunication serviceCommunication, CancellationToken token)
		{
			var service = new TService();

			service.Start(location, invokeData, serviceCommunication);

			return new ValueTask<IService>(service);
		}

	#endregion
	}
}