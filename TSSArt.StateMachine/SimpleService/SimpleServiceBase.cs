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

		private bool _disposed;

		protected SimpleServiceBase()
		{
			Source = null!;
			RawContent = null!;
			ServiceCommunication = null!;
		}

		protected Uri?                  Source               { get; private set; }
		protected string?               RawContent           { get; private set; }
		protected DataModelValue        Content              { get; private set; }
		protected DataModelValue        Parameters           { get; private set; }
		protected IServiceCommunication ServiceCommunication { get; private set; }

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

		internal void Start(Uri? source, string? rawContent, DataModelValue content, DataModelValue parameters, IServiceCommunication serviceCommunication)
		{
			Source = source;
			RawContent = rawContent;
			Content = content;
			Parameters = parameters;
			ServiceCommunication = serviceCommunication;

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