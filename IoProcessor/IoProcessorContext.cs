using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class IoProcessorContext : IAsyncDisposable, IDisposable
	{
		private static readonly Uri ParentTarget = new Uri(uriString: "#_parent", UriKind.Relative);

		private readonly IoProcessor                                                         _ioProcessor;
		private readonly IoProcessorOptions                                                  _options;
		private readonly ConcurrentDictionary<Uri, IService>                                 _parentServiceByTarget    = new ConcurrentDictionary<Uri, IService>();
		private readonly ConcurrentDictionary<(string SessionId, string InvokeId), IService> _serviceByInvokeId        = new ConcurrentDictionary<(string SessionId, string InvokeId), IService>();
		private readonly ConcurrentDictionary<Uri, IService>                                 _serviceByTarget          = new ConcurrentDictionary<Uri, IService>();
		private readonly ConcurrentDictionary<string, StateMachineController>                _stateMachinesBySessionId = new ConcurrentDictionary<string, StateMachineController>();

		public IoProcessorContext(IoProcessor ioProcessor, in IoProcessorOptions options)
		{
			_ioProcessor = ioProcessor;
			_options = options;
		}

		public virtual ValueTask DisposeAsync() => default;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) { }

		public virtual ValueTask Initialize() => default;

		private IStateMachine GetStateMachine(Uri source) => _options.StateMachineProvider.GetStateMachine(source);

		private IStateMachine GetStateMachine(string scxml) => _options.StateMachineProvider.GetStateMachine(scxml);

		private void FillInterpreterOptions(out InterpreterOptions options)
		{
			options = new InterpreterOptions
					  {
							  PersistenceLevel = _options.PersistenceLevel,
							  ResourceLoader = _options.ResourceLoader,
							  StopToken = _options.StopToken,
							  SuspendToken = _options.SuspendToken
					  };

			if (_options.DataModelHandlerFactories != null)
			{
				options.DataModelHandlerFactories = new List<IDataModelHandlerFactory>(_options.DataModelHandlerFactories);
			}
		}

		private static void ValidateTrue(bool result)
		{
			if (result)
			{
				return;
			}

			throw new ApplicationException("Validation failed. Result of operation must be true.");
		}

		protected virtual StateMachineController CreateStateMachineController(string sessionId, IStateMachine stateMachine, in InterpreterOptions options) =>
				new StateMachineController(sessionId, stateMachine, _ioProcessor, _options.SuspendIdlePeriod, options);

		public virtual ValueTask<StateMachineController> CreateAndAddStateMachine(string sessionId, Uri source, DataModelValue content, DataModelValue parameters)
		{
			FillInterpreterOptions(out var options);
			options.Parameters = parameters;

			var stateMachine = source != null ? GetStateMachine(source) : GetStateMachine(content.AsString());
			var stateMachineController = CreateStateMachineController(sessionId, stateMachine, options);
			ValidateTrue(_stateMachinesBySessionId.TryAdd(sessionId, stateMachineController));
			ValidateTrue(_parentServiceByTarget.TryAdd(_ioProcessor.GetTarget(sessionId), stateMachineController));

			return new ValueTask<StateMachineController>(stateMachineController);
		}

		public virtual ValueTask DestroyStateMachine(string sessionId)
		{
			ValidateTrue(_stateMachinesBySessionId.TryRemove(sessionId, out var stateMachineController));
			ValidateTrue(_parentServiceByTarget.TryRemove(_ioProcessor.GetTarget(sessionId), out _));

			return stateMachineController.DisposeAsync();
		}

		public void ValidateSessionId(string sessionId, out StateMachineController controller)
		{
			ValidateTrue(_stateMachinesBySessionId.TryGetValue(sessionId, out controller));
		}

		public virtual ValueTask AddService(string sessionId, string invokeId, IService service)
		{
			ValidateTrue(_serviceByInvokeId.TryAdd((sessionId, invokeId), service));

			if (service is StateMachineController stateMachineController)
			{
				ValidateTrue(_serviceByTarget.TryAdd(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), service));
			}

			ValidateTrue(_serviceByTarget.TryAdd(new Uri("#_" + invokeId, UriKind.Relative), service));

			return default;
		}

		public virtual ValueTask<IService> TryCompleteService(string sessionId, string invokeId)
		{
			if (!_serviceByInvokeId.TryGetValue((sessionId, invokeId), out var service))
			{
				return new ValueTask<IService>((IService) null);
			}

			if (!_serviceByInvokeId.TryUpdate((sessionId, invokeId), newValue: null, service))
			{
				return new ValueTask<IService>((IService) null);
			}

			if (service is StateMachineController stateMachineController)
			{
				ValidateTrue(_serviceByTarget.TryRemove(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), out _));
			}

			ValidateTrue(_serviceByTarget.TryRemove(new Uri("#_" + invokeId, UriKind.Relative), out _));

			return new ValueTask<IService>(service);
		}

		public virtual ValueTask<IService> TryRemoveService(string sessionId, string invokeId)
		{
			if (!_serviceByInvokeId.TryRemove((sessionId, invokeId), out var service) || service == null)
			{
				return new ValueTask<IService>((IService) null);
			}

			if (service is StateMachineController stateMachineController)
			{
				ValidateTrue(_serviceByTarget.TryRemove(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), out _));
			}

			ValidateTrue(_serviceByTarget.TryRemove(new Uri("#_" + invokeId, UriKind.Relative), out _));

			return new ValueTask<IService>(service);
		}

		public bool TryGetService(string sessionId, string invokeId, out IService service) => _serviceByInvokeId.TryGetValue((sessionId, invokeId), out service);

		public IService GetService(Uri origin, Uri target)
		{
			var result = target == ParentTarget
					? _parentServiceByTarget.TryGetValue(origin, out var service)
					: _serviceByTarget.TryGetValue(target, out service);

			if (result)
			{
				return service;
			}

			var sessionId = ExtractSessionId(target);

			if (_stateMachinesBySessionId.TryGetValue(sessionId, out var stateMachineController))
			{
				return stateMachineController;
			}

			throw new ApplicationException("Cannot find target");
		}

		private static string ExtractSessionId(Uri target) => Path.GetFileName(target.LocalPath);
	}
}