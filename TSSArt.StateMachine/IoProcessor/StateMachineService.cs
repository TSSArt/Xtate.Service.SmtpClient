using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class StateMachineService : IService
	{
		private readonly Channel<IEvent>                 _channel;
		private readonly CancellationTokenSource         _destroyTokenSource = new CancellationTokenSource();
		private readonly IService                        _parentService;
		private readonly IStateMachine                   _stateMachine;
		private readonly InterpreterOptions              _options;
		private readonly DataModelValue                  _arguments;
		private readonly IExternalCommunication          _externalCommunication;
		private readonly Action<StateMachineService>     _onStateMachineCompleted;
		private readonly TimeSpan _idlePeriod;
		private readonly HashSet<ScheduledEvent>         _scheduledEvents   = new HashSet<ScheduledEvent>();
		private readonly Dictionary<string, IService>    _serviceByInvokeId = new Dictionary<string, IService>();
		private readonly ConcurrentQueue<ScheduledEvent> _toDelete          = new ConcurrentQueue<ScheduledEvent>();
		private readonly TaskCompletionSource<object>    _acceptedTcs        = new TaskCompletionSource<object>();
		private          CancellationTokenSource         _suspendOnIdle;

		public StateMachineService(IService parentService, string sessionId, IStateMachine stateMachine, InterpreterOptions options, DataModelValue arguments,
								   IExternalCommunication externalCommunication, Action<StateMachineService> onStateMachineCompleted, TimeSpan idlePeriod)
		{
			SessionId = sessionId;
			_parentService = parentService;
			_stateMachine = stateMachine;
			_options = options;
			_arguments = arguments;
			_externalCommunication = externalCommunication;
			_onStateMachineCompleted = onStateMachineCompleted;
			_idlePeriod = idlePeriod;
			_channel = Channel.CreateUnbounded<IEvent>();
		}

		public string             SessionId { get; }
		public StateMachineResult Result    { get; private set; }

		public Task StartAsync(CancellationToken token)
		{
			token.Register(() => _acceptedTcs.TrySetCanceled(token));

			RunAsync();

			return _acceptedTcs.Task;
		}

		private async void RunAsync()
		{
			var ready = true;
			while (ready)
			{
				try
				{
					_suspendOnIdle = _idlePeriod > TimeSpan.Zero ? new CancellationTokenSource(_idlePeriod) : null;

					Result = await StateMachineInterpreter.RunAsync(SessionId, _stateMachine, _channel.Reader, _options, _arguments, 
																	_destroyTokenSource.Token, _suspendOnIdle?.Token ?? default).ConfigureAwait(false);
					_acceptedTcs.TrySetResult(null);

					if (Result.Status == StateMachineExitStatus.Completed || Result.Status == StateMachineExitStatus.LiveLockAbort)
					{
						_onStateMachineCompleted(this);

						return;
					}
				}
				catch (OperationCanceledException ex)
				{
					_acceptedTcs.TrySetCanceled(ex.CancellationToken);
				}
				catch (Exception ex)
				{
					_acceptedTcs.TrySetException(ex);
				}

				ready = await _channel.Reader.WaitToReadAsync();
			}
		}

		public Task Send(IEvent @event, CancellationToken token) => _channel.Writer.WriteAsync(@event, token).AsTask();

		public Task Destroy(CancellationToken token)
		{
			_channel.Writer.Complete();
			_destroyTokenSource.Cancel();
			return Task.CompletedTask;
		}

		public void OnStateChanged(StateMachineInterpreterState state)
		{
			if (state == StateMachineInterpreterState.Accepted)
			{
				_acceptedTcs.TrySetResult(null);
			}
			else if (state == StateMachineInterpreterState.Waiting)
			{
				_suspendOnIdle?.CancelAfter(_idlePeriod);
			}
		}

		public void RegisterService(string invokeId, IService service)
		{
			if (!_serviceByInvokeId.TryAdd(invokeId, service))
			{
				throw new ApplicationException("InvokeId already exists");
			}
		}

		public IService UnregisterService(string invokeId)
		{
			if (_serviceByInvokeId.Remove(invokeId, out var service))
			{
				return service;
			}

			throw new ApplicationException("InvokeId does not exist");
		}

		public Task ForwardEvent(string invokeId, IEvent @event, in CancellationToken token)
		{
			if (!_serviceByInvokeId.TryGetValue(invokeId, out var service))
			{
				throw new ApplicationException("Invalid InvokeId");
			}

			return service.Send(@event, token);
		}

		public void ScheduleEvent(IEvent @event, Uri type, Uri target, int delayMs)
		{
			var scheduledEvent = new ScheduledEvent(@event, type, target, delayMs);

			_scheduledEvents.Add(scheduledEvent);

			Task.Delay(delayMs, CancellationToken.None).ContinueWith(Fire, @event);

			CleanScheduledEvents();
		}

		private Task Fire(Task delayTask, object eventObj)
		{
			var scheduledEvent = (ScheduledEvent) eventObj;

			if (delayTask.IsCompletedSuccessfully && !scheduledEvent.IsDisposed)
			{
				return _externalCommunication.SendEvent(SessionId, scheduledEvent.Event, scheduledEvent.Type, scheduledEvent.Target, delayMs: 0, CancellationToken.None);
			}

			return Task.CompletedTask;
		}

		public void CancelEvent(string sendId)
		{
			foreach (var @event in _scheduledEvents)
			{
				if (@event.SendId == sendId)
				{
					DisposeEvent(@event);
				}
			}

			CleanScheduledEvents();
		}

		public Task ReturnDoneEvent(DataModelValue doneData, CancellationToken token)
		{
			var eventObject = new EventObject(EventType.External, null, "TBD", "invokeid", null, null, doneData); //TODO: put correct parameters

			return _parentService.Send(eventObject, token);
		}

		private void CleanScheduledEvents()
		{
			while (_toDelete.TryDequeue(out var @event))
			{
				_scheduledEvents.Remove(@event);
			}
		}

		private void DisposeEvent(ScheduledEvent @event)
		{
			_toDelete.Enqueue(@event);

			@event.Dispose();
		}
	}
}