using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	internal class StateMachineHostContext : IAsyncDisposable
	{
		private static readonly Uri ParentTarget = new Uri(uriString: "#_parent", UriKind.Relative);

		private static readonly XmlReaderSettings DefaultSyncXmlReaderSettings  = new XmlReaderSettings { Async = false, CloseInput = true };
		private static readonly XmlReaderSettings DefaultAsyncXmlReaderSettings = new XmlReaderSettings { Async = true, CloseInput = true };
		private readonly        DataModelObject?  _configuration;

		private readonly ImmutableDictionary<object, object>?   _contextRuntimeItems;
		private readonly StateMachineHostOptions                _options;
		private readonly ConcurrentDictionary<string, IService> _parentServiceBySessionId = new ConcurrentDictionary<string, IService>();

		private readonly ConcurrentDictionary<(string SessionId, string InvokeId), (string InvokeUniqueId, IService? Service)> _serviceByInvokeId =
				new ConcurrentDictionary<(string SessionId, string InvokeId), (string InvokeUniqueId, IService? Service)>();

		private readonly ConcurrentDictionary<Uri, IService>                  _serviceByTarget = new ConcurrentDictionary<Uri, IService>(FullUriComparer.Instance);
		private readonly IStateMachineHost                                    _stateMachineHost;
		private readonly ConcurrentDictionary<string, StateMachineController> _stateMachinesBySessionId = new ConcurrentDictionary<string, StateMachineController>();
		private readonly CancellationTokenSource                              _stopTokenSource;
		private readonly CancellationTokenSource                              _suspendTokenSource;

		public StateMachineHostContext(IStateMachineHost stateMachineHost, StateMachineHostOptions options)
		{
			_stateMachineHost = stateMachineHost;
			_options = options;
			_suspendTokenSource = new CancellationTokenSource();
			_stopTokenSource = new CancellationTokenSource();

			if (stateMachineHost is IHost host)
			{
				_contextRuntimeItems = ImmutableDictionary<object, object>.Empty.Add(typeof(IHost), host);
			}

			if (options.Configuration?.Count > 0)
			{
				_configuration = options.Configuration.ToDataModelObject(p => p.Key, p => p.Value);
				_configuration.MakeDeepConstant();
			}
		}

		protected CancellationToken StopToken => _stopTokenSource.Token;

	#region Interface IAsyncDisposable

		public virtual ValueTask DisposeAsync()
		{
			_suspendTokenSource.Dispose();
			_stopTokenSource.Dispose();

			return default;
		}

	#endregion

		public virtual ValueTask InitializeAsync(CancellationToken token) => default;

		private void FillInterpreterOptions(out InterpreterOptions interpreterOptions)
		{
			interpreterOptions = new InterpreterOptions
								 {
										 Configuration = _configuration,
										 PersistenceLevel = _options.PersistenceLevel,
										 StorageProvider = _options.StorageProvider,
										 ResourceLoaders = _options.ResourceLoaders,
										 CustomActionProviders = _options.CustomActionFactories,
										 StopToken = _stopTokenSource.Token,
										 SuspendToken = _suspendTokenSource.Token,
										 Logger = _options.Logger,
										 DataModelHandlerFactories = _options.DataModelHandlerFactories,
										 ContextRuntimeItems = _contextRuntimeItems
								 };
		}

		private static void ValidateTrue(bool result)
		{
			var condition = result;

			Infrastructure.Assert(condition, Resources.Assertion_ValidationFailed);
		}

		protected virtual StateMachineController CreateStateMachineController(string sessionId, IStateMachine? stateMachine,
																			  IStateMachineOptions? stateMachineOptions,
																			  Uri? stateMachineLocation, in InterpreterOptions defaultOptions) =>
				new StateMachineController(sessionId, stateMachineOptions, stateMachine, stateMachineLocation, _stateMachineHost, _options.SuspendIdlePeriod, defaultOptions);

		private static XmlReaderSettings GetXmlReaderSettings(bool useAsync = false) => useAsync ? DefaultAsyncXmlReaderSettings : DefaultSyncXmlReaderSettings;

		private XmlParserContext GetXmlParserContext()
		{
			var xmlNameTable = new NameTable();
			var nameTable = xmlNameTable;
			ScxmlDirector.FillXmlNameTable(nameTable);

			if (!_options.CustomActionFactories.IsDefaultOrEmpty)
			{
				foreach (var factory in _options.CustomActionFactories)
				{
					factory.FillXmlNameTable(nameTable);
				}
			}

			var nsManager = new XmlNamespaceManager(nameTable);
			return new XmlParserContext(nameTable, nsManager, xmlLang: null, xmlSpace: default);
		}

		private static IBuilderFactory GetBuilderFactory() => BuilderFactory.Instance;

		private IStateMachine GetStateMachine(string scxml, IErrorProcessor errorProcessor)
		{
			using var stringReader = new StringReader(scxml);
			using var xmlReader = XmlReader.Create(stringReader, GetXmlReaderSettings(), GetXmlParserContext());
			var scxmlDirector = new ScxmlDirector(xmlReader, GetBuilderFactory(), errorProcessor);

			return scxmlDirector.ConstructStateMachine();
		}

		private async ValueTask<IStateMachine> GetStateMachine(Uri source, IErrorProcessor errorProcessor, CancellationToken token)
		{
			if (!_options.ResourceLoaders.IsDefaultOrEmpty)
			{
				foreach (var resourceLoader in _options.ResourceLoaders)
				{
					if (resourceLoader.CanHandle(source))
					{
						using var xmlReader = await resourceLoader.RequestXmlReader(source, GetXmlReaderSettings(), GetXmlParserContext(), token).ConfigureAwait(false);
						var scxmlDirector = new ScxmlDirector(xmlReader, GetBuilderFactory(), errorProcessor);

						return scxmlDirector.ConstructStateMachine();
					}
				}
			}

			throw new StateMachineProcessorException(Resources.Exception_Cannot_find_ResourceLoader_to_load_external_resource);
		}

		protected async ValueTask<(IStateMachine StateMachine, Uri? Location)> LoadStateMachine(StateMachineOrigin origin, Uri? hostBaseUri, IErrorProcessor errorProcessor, CancellationToken token)
		{
			var location = CombineUri(hostBaseUri, origin.BaseUri);

			switch (origin.Type)
			{
				case StateMachineOriginType.StateMachine:
					return (origin.AsStateMachine(), location);

				case StateMachineOriginType.Scxml:
					return (GetStateMachine(origin.AsScxml(), errorProcessor), location);

				case StateMachineOriginType.Source:
					location = CombineUri(location, origin.AsSource());
					var stateMachine = await GetStateMachine(location, errorProcessor, token).ConfigureAwait(false);
					return (stateMachine, location);

				default:
					throw new ArgumentException(Resources.Exception_StateMachine_origin_missed);
			}
		}

		[return: NotNullIfNotNull("relativeUri")]
		private static Uri? CombineUri(Uri? baseUri, Uri? relativeUri)
		{
			if (baseUri != null && baseUri.IsAbsoluteUri && relativeUri != null && !relativeUri.IsAbsoluteUri)
			{
				return new Uri(baseUri, relativeUri);
			}

			return relativeUri;
		}

		public virtual async ValueTask<StateMachineController> CreateAndAddStateMachine(string sessionId, StateMachineOrigin origin, DataModelValue parameters,
																						IErrorProcessor errorProcessor, CancellationToken token)
		{
			var (stateMachine, location) = await LoadStateMachine(origin, _options.BaseUri, errorProcessor, token).ConfigureAwait(false);

			FillInterpreterOptions(out var interpreterOptions);
			interpreterOptions.Arguments = parameters;
			interpreterOptions.ErrorProcessor = errorProcessor;
			interpreterOptions.Host = CreateHostData(location);

			stateMachine.Is<IStateMachineOptions>(out var stateMachineOptions);

			var stateMachineController = CreateStateMachineController(sessionId, stateMachine, stateMachineOptions, location, interpreterOptions);
			RegisterStateMachineController(stateMachineController);

			return stateMachineController;
		}

		protected StateMachineController AddSavedStateMachine(string sessionId, Uri? stateMachineLocation, IStateMachineOptions stateMachineOptions, IErrorProcessor errorProcessor)
		{
			FillInterpreterOptions(out var interpreterOptions);
			interpreterOptions.ErrorProcessor = errorProcessor;
			interpreterOptions.Host = CreateHostData(stateMachineLocation);

			var stateMachineController = CreateStateMachineController(sessionId, stateMachine: default, stateMachineOptions, stateMachineLocation, interpreterOptions);
			RegisterStateMachineController(stateMachineController);

			return stateMachineController;
		}

		private static DataModelObject? CreateHostData(Uri? stateMachineLocation)
		{
			if (stateMachineLocation != null)
			{
				var obj = new DataModelObject { ["location"] = stateMachineLocation.ToString() };
				obj.MakeDeepConstant();

				return obj;
			}

			return null;
		}

		private void RegisterStateMachineController(StateMachineController stateMachineController)
		{
			var sessionId = stateMachineController.SessionId;
			ValidateTrue(_stateMachinesBySessionId.TryAdd(sessionId, stateMachineController));
			ValidateTrue(_serviceByTarget.TryAdd(new Uri("#_scxml_" + sessionId, UriKind.Relative), stateMachineController));
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

		public virtual ValueTask AddService(string sessionId, string invokeId, string invokeUniqueId, IService service, CancellationToken token)
		{
			ValidateTrue(_serviceByInvokeId.TryAdd((sessionId, invokeId), (invokeUniqueId, service)));

			if (service is StateMachineController stateMachineController)
			{
				if (_stateMachinesBySessionId.TryGetValue(sessionId, out var controller))
				{
					ValidateTrue(_parentServiceBySessionId.TryAdd(stateMachineController.SessionId, controller));
				}
			}

			ValidateTrue(_serviceByTarget.TryAdd(new Uri("#_" + invokeId, UriKind.Relative), service));

			return default;
		}

		public virtual ValueTask<IService?> TryCompleteService(string sessionId, string invokeId)
		{
			if (!_serviceByInvokeId.TryGetValue((sessionId, invokeId), out var pair))
			{
				return new ValueTask<IService?>((IService?) null);
			}

			if (!_serviceByInvokeId.TryUpdate((sessionId, invokeId), (pair.InvokeUniqueId, null), pair))
			{
				return new ValueTask<IService?>((IService?) null);
			}

			if (pair.Service is StateMachineController stateMachineController)
			{
				_parentServiceBySessionId.TryRemove(stateMachineController.SessionId, out _);
			}

			_serviceByTarget.TryRemove(new Uri("#_" + invokeId, UriKind.Relative), out _);

			return new ValueTask<IService?>(pair.Service);
		}

		public virtual ValueTask<IService?> TryRemoveService(string sessionId, string invokeId)
		{
			if (!_serviceByInvokeId.TryRemove((sessionId, invokeId), out var pair) || pair.Service == null)
			{
				return new ValueTask<IService?>((IService?) null);
			}

			if (pair.Service is StateMachineController stateMachineController)
			{
				_parentServiceBySessionId.TryRemove(stateMachineController.SessionId, out _);
			}

			_serviceByTarget.TryRemove(new Uri("#_" + invokeId, UriKind.Relative), out _);

			return new ValueTask<IService?>(pair.Service);
		}

		public bool TryGetService(string sessionId, string invokeId, out (string InvokeUniqueId, IService? Service) pair) => _serviceByInvokeId.TryGetValue((sessionId, invokeId), out pair);

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

			throw new StateMachineProcessorException(Resources.Exception_Cannot_find_target);
		}

		public async ValueTask WaitAllAsync(CancellationToken token)
		{
			var exit = false;

			while (!exit)
			{
				exit = true;

				foreach (var pair in _stateMachinesBySessionId)
				{
					var controller = pair.Value;
					if (controller.Result.IsCompleted)
					{
						continue;
					}

					try
					{
						await controller.Result.WaitAsync(token).ConfigureAwait(false);
					}
					catch (OperationCanceledException ex) when (ex.CancellationToken == token)
					{
						throw;
					}
					catch
					{
						// ignored
					}

					exit = false;
				}
			}
		}

		public void Stop() => _stopTokenSource.Cancel();

		public void Suspend() => _suspendTokenSource.Cancel();
	}
}