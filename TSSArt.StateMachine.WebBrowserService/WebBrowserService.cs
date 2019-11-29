using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.Services
{
	public abstract class WebBrowserService : IService
	{
		private static readonly Uri TypeId      = new Uri("http://tssart.com/scxml/service/browser");
		private static readonly Uri AliasTypeId = new Uri(uriString: "browser", UriKind.Relative);

		private readonly TaskCompletionSource<DataModelValue> _completedTcs = new TaskCompletionSource<DataModelValue>();
		private readonly CancellationTokenSource              _tokenSource  = new CancellationTokenSource();

		protected Uri            Source     { get; private set; }
		protected DataModelValue Parameters { get; private set; }

		ValueTask IService.Send(IEvent @event, CancellationToken token) => default;

		ValueTask IService.Destroy(CancellationToken token)
		{
			_tokenSource.Cancel();
			_completedTcs.TrySetCanceled();

			return default;
		}

		ValueTask<DataModelValue> IService.Result => new ValueTask<DataModelValue>(_completedTcs.Task);

		public static IServiceFactory GetFactory<T>() where T : WebBrowserService, new() => new Factory<T>();

		private void Start(Uri source, DataModelValue parameters)
		{
			Source = source;
			Parameters = parameters;

			RunAsync();

			async void RunAsync()
			{
				try
				{
					_completedTcs.TrySetResult(await Execute().ConfigureAwait(false));
				}
				catch (OperationCanceledException ex)
				{
					_completedTcs.TrySetCanceled(ex.CancellationToken);
				}
				catch (Exception ex)
				{
					_completedTcs.TrySetException(ex);
				}
			}
		}

		protected abstract ValueTask<DataModelValue> Execute();

		private class Factory<T> : IServiceFactory where T : WebBrowserService, new()
		{
			Uri IServiceFactory.TypeId      => TypeId;
			Uri IServiceFactory.AliasTypeId => AliasTypeId;

			public ValueTask<IService> StartService(Uri source, DataModelValue content, DataModelValue parameters, IServiceCommunication serviceCommunication, CancellationToken token)
			{
				var service = new T();

				service.Start(source, parameters);

				return new ValueTask<IService>(service);
			}
		}
	}
}