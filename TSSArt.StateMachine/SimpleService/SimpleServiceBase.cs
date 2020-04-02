using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public abstract class SimpleServiceBase : IService, IAsyncDisposable
	{
		private readonly TaskCompletionSource<DataModelValue> _completedTcs = new TaskCompletionSource<DataModelValue>();
		private readonly CancellationTokenSource              _tokenSource  = new CancellationTokenSource();

		private bool        _disposed;
		private InvokeData? _invokeData;

		protected SimpleServiceBase()
		{
			_invokeData = null!;
			ServiceCommunication = null!;
		}

		protected Uri?                  Location             { get; private set; }
		protected IServiceCommunication ServiceCommunication { get; private set; }

		protected Uri?           Source     => _invokeData?.Source;
		protected string?        RawContent => _invokeData?.RawContent;
		protected DataModelValue Content    => _invokeData?.Content ?? default;
		protected DataModelValue Parameters => _invokeData?.Parameters ?? default;

		protected CancellationToken StopToken => _tokenSource.Token;

	#region Interface IAsyncDisposable

		public async ValueTask DisposeAsync()
		{
			await DisposeAsync(true).ConfigureAwait(false);
		}

	#endregion

	#region Interface IService

		ValueTask IService.Send(IEvent evt, CancellationToken token) => default;

		ValueTask IService.Destroy(CancellationToken token)
		{
			_tokenSource.Cancel();
			_completedTcs.TrySetCanceled();

			return default;
		}

		Task<DataModelValue> IService.Result => _completedTcs.Task;

	#endregion

		internal void Start(Uri? location, InvokeData invokeData, IServiceCommunication serviceCommunication)
		{
			Location = location;
			_invokeData = invokeData;
			ServiceCommunication = serviceCommunication;

			RunAsync().Forget();

			async ValueTask RunAsync()
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

		protected virtual ValueTask DisposeAsync(bool disposing)
		{
			if (_disposed)
			{
				return default;
			}

			if (disposing)
			{
				_tokenSource.Dispose();
			}

			_disposed = true;

			return default;
		}
	}
}