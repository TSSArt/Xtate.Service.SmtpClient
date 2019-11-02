using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class StateMachineController : IService, IExternalCommunication, ILogger, IStorageProvider, INotifyStateChanged
	{
		private readonly TaskCompletionSource<object>         _acceptedTcs = new TaskCompletionSource<object>();
		private readonly Channel<IEvent>                      _channel;
		private readonly TaskCompletionSource<DataModelValue> _completedTcs = new TaskCompletionSource<DataModelValue>();
		private readonly InterpreterOptions                   _defaultOptions;
		private readonly CancellationTokenSource              _destroyTokenSource = new CancellationTokenSource();
		private readonly TimeSpan                             _idlePeriod;
		private readonly IoProcessor                          _ioProcessor;
		private readonly HashSet<ScheduledEvent>              _scheduledEvents = new HashSet<ScheduledEvent>();
		private readonly IStateMachine                        _stateMachine;
		private readonly ConcurrentQueue<ScheduledEvent>      _toDelete = new ConcurrentQueue<ScheduledEvent>();
		private          CancellationTokenSource              _suspendOnIdleTokenSource;

		public StateMachineController(string sessionId, IStateMachine stateMachine, IoProcessor ioProcessor, TimeSpan idlePeriod, in InterpreterOptions defaultOptions)
		{
			SessionId = sessionId;
			_stateMachine = stateMachine;
			_ioProcessor = ioProcessor;
			_defaultOptions = defaultOptions;
			_idlePeriod = idlePeriod;
			_channel = Channel.CreateUnbounded<IEvent>();
		}

		public string SessionId { get; }

		IReadOnlyList<IEventProcessor> IExternalCommunication.GetIoProcessors() => _ioProcessor.GetIoProcessors();

		async ValueTask<SendStatus> IExternalCommunication.TrySendEvent(IOutgoingEvent @event, CancellationToken token)
		{
			var sendStatus = await _ioProcessor.DispatchEvent(SessionId, @event, token).ConfigureAwait(false);

			if (sendStatus == SendStatus.ToSchedule)
			{
				ScheduleEvent(@event);

				return SendStatus.Sent;
			}

			return sendStatus;
		}

		ValueTask IExternalCommunication.CancelEvent(string sendId, CancellationToken token)
		{
			foreach (var @event in _scheduledEvents)
			{
				if (@event.Event.SendId == sendId)
				{
					DisposeEvent(@event);
				}
			}

			CleanScheduledEvents();

			return default;
		}

		ValueTask IExternalCommunication.StartInvoke(string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token) =>
				_ioProcessor.StartInvoke(SessionId, invokeId, type, source, data, token);

		ValueTask IExternalCommunication.CancelInvoke(string invokeId, CancellationToken token) => _ioProcessor.CancelInvoke(SessionId, invokeId, token);

		ValueTask IExternalCommunication.ForwardEvent(IEvent @event, string invokeId, CancellationToken token) => _ioProcessor.ForwardEvent(SessionId, @event, invokeId, token);

		ValueTask ILogger.Log(string stateMachineName, string label, DataModelValue data, CancellationToken token) => _ioProcessor.Log(SessionId, stateMachineName, label, data, token);

		ValueTask ILogger.Error(ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token) =>
				_ioProcessor.Error(SessionId, errorType, stateMachineName, sourceEntityId, exception, token);

		ValueTask INotifyStateChanged.OnChanged(StateMachineInterpreterState state)
		{
			if (state == StateMachineInterpreterState.Accepted)
			{
				_acceptedTcs.TrySetResult(null);
			}
			else if (state == StateMachineInterpreterState.Waiting)
			{
				_suspendOnIdleTokenSource?.CancelAfter(_idlePeriod);
			}

			return default;
		}

		public ValueTask Send(IEvent @event, CancellationToken token) => _channel.Writer.WriteAsync(@event, token);

		ValueTask IService.Destroy(CancellationToken token)
		{
			_channel.Writer.Complete();
			_destroyTokenSource.Cancel();
			return default;
		}

		public ValueTask<DataModelValue> Result => new ValueTask<DataModelValue>(_completedTcs.Task);

		ValueTask<ITransactionalStorage> IStorageProvider.GetTransactionalStorage(string name, CancellationToken token) => _ioProcessor.GetTransactionalStorage(SessionId, name, token);

		ValueTask IStorageProvider.RemoveTransactionalStorage(string name, CancellationToken token) => _ioProcessor.RemoveTransactionalStorage(SessionId, name, token);

		ValueTask IStorageProvider.RemoveAllTransactionalStorage(CancellationToken token) => _ioProcessor.RemoveAllTransactionalStorage(SessionId, token);

		public ValueTask StartAsync(CancellationToken token)
		{
			token.Register(() => _acceptedTcs.TrySetCanceled(token));

			var _ = RunAsync(false);

			return new ValueTask(_acceptedTcs.Task);
		}

		private void FillOptions(out InterpreterOptions options)
		{
			options = _defaultOptions;

			options.ExternalCommunication = this;
			options.Logger = this;
			options.StorageProvider = this;
			options.NotifyStateChanged = this;

			if (_idlePeriod > TimeSpan.Zero)
			{
				_suspendOnIdleTokenSource = new CancellationTokenSource(_idlePeriod);

				options.SuspendToken = options.SuspendToken.CanBeCanceled
						? CancellationTokenSource.CreateLinkedTokenSource(options.SuspendToken, _suspendOnIdleTokenSource.Token).Token
						: _suspendOnIdleTokenSource.Token;
			}

			options.DestroyToken = options.DestroyToken.CanBeCanceled
					? CancellationTokenSource.CreateLinkedTokenSource(options.DestroyToken, _destroyTokenSource.Token).Token
					: _destroyTokenSource.Token;
		}

		public async ValueTask<DataModelValue> ExecuteAsync() => (await RunAsync(true).ConfigureAwait(false)).Result;

		private async ValueTask<StateMachineResult> RunAsync(bool throwOnError)
		{
			var exitStatus = StateMachineExitStatus.Unknown;
			while (true)
			{
				try
				{
					FillOptions(out var options);
					var result = await StateMachineInterpreter.RunAsync(SessionId, _stateMachine, _channel.Reader, options).ConfigureAwait(false);
					exitStatus = result.Status;

					_acceptedTcs.TrySetResult(null);

					switch (result.Status)
					{
						case StateMachineExitStatus.Completed:
							_completedTcs.TrySetResult(result.Result);
							return new StateMachineResult(StateMachineExitStatus.Completed, result.Result);

						case StateMachineExitStatus.Suspended:
							break;

						case StateMachineExitStatus.Destroyed:
							var exception = new OperationCanceledException(options.DestroyToken);
							if (throwOnError)
							{
								throw exception;
							}

							_completedTcs.TrySetCanceled(options.DestroyToken);
							return new StateMachineResult(result.Status, exception);

						case StateMachineExitStatus.QueueClosed:
						case StateMachineExitStatus.LiveLockAbort:
							if (throwOnError)
							{
								throw result.Exception;
							}

							_completedTcs.TrySetException(result.Exception);
							return new StateMachineResult(result.Status, result.Exception);

						default: throw new ArgumentOutOfRangeException();
					}

					if (!await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
					{
						exitStatus = StateMachineExitStatus.QueueClosed;
						await _channel.Reader.ReadAsync().ConfigureAwait(false);
					}
				}
				catch (Exception ex)
				{
					if (ex is OperationCanceledException operationCanceledException)
					{
						_acceptedTcs.TrySetCanceled(operationCanceledException.CancellationToken);
						_completedTcs.TrySetCanceled(operationCanceledException.CancellationToken);
					}
					else
					{
						_acceptedTcs.TrySetException(ex);
						_completedTcs.TrySetException(ex);
					}

					if (throwOnError)
					{
						throw;
					}

					return new StateMachineResult(exitStatus, ex);
				}
			}
		}

		private void ScheduleEvent(IOutgoingEvent @event)
		{
			var scheduledEvent = new ScheduledEvent(@event);

			_scheduledEvents.Add(scheduledEvent);

			var _ = DelayedFire(scheduledEvent);

			CleanScheduledEvents();
		}

		private async ValueTask DelayedFire(ScheduledEvent scheduledEvent)
		{
			await Task.Delay(scheduledEvent.Event.DelayMs).ConfigureAwait(false);

			if (!scheduledEvent.IsDisposed)
			{
				try
				{
					DisposeEvent(scheduledEvent);

					await _ioProcessor.DispatchEvent(SessionId, scheduledEvent.Event, CancellationToken.None).ConfigureAwait(false);
				}
				catch
				{
					//TODO: send error.communication event into originator session
				}
			}
		}

		private void CleanScheduledEvents()
		{
			while (_toDelete.TryDequeue(out var scheduledEvent))
			{
				_scheduledEvents.Remove(scheduledEvent);
			}
		}

		private void DisposeEvent(ScheduledEvent scheduledEvent)
		{
			_toDelete.Enqueue(scheduledEvent);

			scheduledEvent.Dispose();
		}
	}
}