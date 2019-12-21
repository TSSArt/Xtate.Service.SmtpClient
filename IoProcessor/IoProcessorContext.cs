using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class IoProcessorContext : IAsyncDisposable, IDisposable
	{
		private static readonly Uri ParentTarget = new Uri(uriString: "#_parent", UriKind.Relative);

		private readonly IIoProcessor       _ioProcessor;
		private readonly IoProcessorOptions _options;

		private readonly ConcurrentDictionary<string, IService> _parentServiceBySessionId = new ConcurrentDictionary<string, IService>();

		private readonly ConcurrentDictionary<(string SessionId, string InvokeId), (string InvokeUniqueId, IService Service)> _serviceByInvokeId =
				new ConcurrentDictionary<(string SessionId, string InvokeId), (string InvokeUniqueId, IService Service)>();

		private readonly ConcurrentDictionary<Uri, IService>                  _serviceByTarget          = new ConcurrentDictionary<Uri, IService>(UriComparer.Instance);
		private readonly ConcurrentDictionary<string, StateMachineController> _stateMachinesBySessionId = new ConcurrentDictionary<string, StateMachineController>();

		public IoProcessorContext(IIoProcessor ioProcessor, in IoProcessorOptions options)
		{
			_ioProcessor = ioProcessor;
			_options = options;
		}

		public virtual ValueTask DisposeAsync()
		{
			Dispose();

			return default;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) { }

		public virtual ValueTask Initialize() => default;

		private void FillInterpreterOptions(out InterpreterOptions options)
		{
			options = new InterpreterOptions
					  {
							  PersistenceLevel = _options.PersistenceLevel,
							  ResourceLoader = _options.ResourceLoader,
							  CustomActionProviders = _options.CustomActionProviders,
							  StopToken = _options.StopToken,
							  SuspendToken = _options.SuspendToken,
							  Logger = _options.Logger
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

		public virtual async ValueTask<StateMachineController> CreateAndAddStateMachine(string sessionId, IStateMachine stateMachine, Uri source, DataModelValue content, DataModelValue parameters)
		{
			FillInterpreterOptions(out var options);
			options.Arguments = parameters;

			if (stateMachine == null)
			{
				stateMachine = source != null
						? await _options.StateMachineProvider.GetStateMachine(source).ConfigureAwait(false)
						: await _options.StateMachineProvider.GetStateMachine(content.AsString()).ConfigureAwait(false);
			}

			var stateMachineController = CreateStateMachineController(sessionId, stateMachine, options);
			ValidateTrue(_stateMachinesBySessionId.TryAdd(sessionId, stateMachineController));
			ValidateTrue(_serviceByTarget.TryAdd(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), stateMachineController));

			return stateMachineController;
		}

		public virtual ValueTask DestroyStateMachine(string sessionId)
		{
			ValidateTrue(_stateMachinesBySessionId.TryRemove(sessionId, out var stateMachineController));
			ValidateTrue(_serviceByTarget.TryRemove(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), out _));

			return stateMachineController.DisposeAsync();
		}

		public void ValidateSessionId(string sessionId, out StateMachineController controller)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));

			ValidateTrue(_stateMachinesBySessionId.TryGetValue(sessionId, out controller));
		}

		public virtual ValueTask AddService(string sessionId, string invokeId, string invokeUniqueId, IService service)
		{
			ValidateTrue(_serviceByInvokeId.TryAdd((sessionId, invokeId), (invokeUniqueId, service)));

			if (service is StateMachineController stateMachineController)
			{
				if (_stateMachinesBySessionId.TryGetValue(sessionId, out var controller))
				{
					ValidateTrue(_parentServiceBySessionId.TryAdd(stateMachineController.SessionId, controller));
				}

				ValidateTrue(_serviceByTarget.TryAdd(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), service));
			}

			ValidateTrue(_serviceByTarget.TryAdd(new Uri("#_" + invokeId, UriKind.Relative), service));

			return default;
		}

		public virtual ValueTask<IService> TryCompleteService(string sessionId, string invokeId)
		{
			if (!_serviceByInvokeId.TryGetValue((sessionId, invokeId), out var pair))
			{
				return new ValueTask<IService>((IService) null);
			}

			if (!_serviceByInvokeId.TryUpdate((sessionId, invokeId), (pair.InvokeUniqueId, null), pair))
			{
				return new ValueTask<IService>((IService) null);
			}

			if (pair.Service is StateMachineController stateMachineController)
			{
				_parentServiceBySessionId.TryRemove(stateMachineController.SessionId, out _);
				_serviceByTarget.TryRemove(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), out _);
			}

			_serviceByTarget.TryRemove(new Uri("#_" + invokeId, UriKind.Relative), out _);

			return new ValueTask<IService>(pair.Service);
		}

		public virtual ValueTask<IService> TryRemoveService(string sessionId, string invokeId)
		{
			if (!_serviceByInvokeId.TryRemove((sessionId, invokeId), out var pair) || pair.Service == null)
			{
				return new ValueTask<IService>((IService) null);
			}

			if (pair.Service is StateMachineController stateMachineController)
			{
				_parentServiceBySessionId.TryRemove(stateMachineController.SessionId, out _);
				_serviceByTarget.TryRemove(new Uri("#_scxml_" + stateMachineController.SessionId, UriKind.Relative), out _);
			}

			_serviceByTarget.TryRemove(new Uri("#_" + invokeId, UriKind.Relative), out _);

			return new ValueTask<IService>(pair.Service);
		}

		public bool TryGetService(string sessionId, string invokeId, out (string InvokeUniqueId, IService Service) pair) => _serviceByInvokeId.TryGetValue((sessionId, invokeId), out pair);

		public IService GetService(string sessionId, Uri target)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
			if (target == null) throw new ArgumentNullException(nameof(target));

			var result = target == ParentTarget
					? _parentServiceBySessionId.TryGetValue(sessionId, out var service)
					: _serviceByTarget.TryGetValue(target, out service);

			if (result)
			{
				return service;
			}

			var targetSessionId = ExtractSessionId(target);

			if (_stateMachinesBySessionId.TryGetValue(targetSessionId, out var stateMachineController))
			{
				return stateMachineController;
			}

			throw new ApplicationException("Cannot find target");
		}

		private static string ExtractSessionId(Uri target) => Path.GetFileName(target.LocalPath);
	}
}