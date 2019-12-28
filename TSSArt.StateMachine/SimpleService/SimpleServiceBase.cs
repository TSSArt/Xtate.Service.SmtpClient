﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public abstract class SimpleServiceBase : IService, IDisposable
	{
		private readonly TaskCompletionSource<ServiceResult> _completedTcs = new TaskCompletionSource<ServiceResult>();
		private readonly CancellationTokenSource             _tokenSource  = new CancellationTokenSource();

		protected Uri                   Source               { get; private set; }
		protected string                RawContent           { get; private set; }
		protected DataModelValue        Content              { get; private set; }
		protected DataModelValue        Parameters           { get; private set; }
		protected IServiceCommunication ServiceCommunication { get; private set; }

		protected CancellationToken StopToken => _tokenSource.Token;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		ValueTask IService.Send(IEvent @event, CancellationToken token) => default;

		ValueTask IService.Destroy(CancellationToken token)
		{
			_tokenSource.Cancel();
			_completedTcs.TrySetCanceled();

			return default;
		}

		ValueTask<ServiceResult> IService.GetResult() => new ValueTask<ServiceResult>(_completedTcs.Task);

		internal void Start(Uri source, string rawContent, DataModelValue content, DataModelValue parameters, IServiceCommunication serviceCommunication)
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

		protected abstract ValueTask<ServiceResult> Execute();

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_tokenSource.Dispose();
			}
		}
	}
}