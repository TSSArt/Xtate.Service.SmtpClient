using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public abstract class SimpleServiceBase : IService, IDisposable
	{
		private readonly TaskCompletionSource<DataModelValue> _completedTcs = new TaskCompletionSource<DataModelValue>();
		private readonly CancellationTokenSource              _tokenSource  = new CancellationTokenSource();

		public void Start(Uri source, DataModelValue arg)
		{
			Source = source;
			Argument = arg;

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

		protected Uri            Source   { get; private set; }
		protected DataModelValue Argument { get; private set; }

		protected CancellationToken StopToken => _tokenSource.Token;

		ValueTask IService.Send(IEvent @event, CancellationToken token) => default;

		ValueTask IService.Destroy(CancellationToken token)
		{
			_tokenSource.Cancel();
			_completedTcs.TrySetCanceled();

			return default;
		}

		ValueTask<DataModelValue> IService.Result => new ValueTask<DataModelValue>(_completedTcs.Task);

		protected abstract ValueTask<DataModelValue> Execute();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_tokenSource.Dispose();
			}
		}
	}
}