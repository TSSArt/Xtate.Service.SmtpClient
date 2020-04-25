using System;
using System.Collections.Immutable;
using System.ComponentModel;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class IoProcessorBuilder
	{
		private ImmutableDictionary<string, string>.Builder?      _configuration;
		private ImmutableArray<ICustomActionFactory>.Builder?     _customActionFactories;
		private ImmutableArray<IDataModelHandlerFactory>.Builder? _dataModelHandlerFactories;
		private ImmutableArray<IEventProcessorFactory>.Builder?   _eventProcessorFactories;
		private ILogger?                                          _logger;
		private PersistenceLevel                                  _persistenceLevel;
		private ImmutableArray<IResourceLoader>.Builder?          _resourceLoaders;
		private ImmutableArray<IServiceFactory>.Builder?          _serviceFactories;
		private IStorageProvider?                                 _storageProvider;
		private TimeSpan                                          _suspendIdlePeriod;
		private bool                                              _verboseValidation = true;

		public IoProcessor Build()
		{
			var option = new IoProcessorOptions
						 {
								 EventProcessorFactories = _eventProcessorFactories?.ToImmutable() ?? default,
								 ServiceFactories = _serviceFactories?.ToImmutable() ?? default,
								 DataModelHandlerFactories = _dataModelHandlerFactories?.ToImmutable() ?? default,
								 CustomActionFactories = _customActionFactories?.ToImmutable() ?? default,
								 ResourceLoaders = _resourceLoaders?.ToImmutable() ?? default,
								 Configuration = _configuration?.ToImmutable(),
								 Logger = _logger,
								 PersistenceLevel = _persistenceLevel,
								 StorageProvider = _storageProvider,
								 SuspendIdlePeriod = _suspendIdlePeriod,
								 VerboseValidation = _verboseValidation
						 };

			return new IoProcessor(option);
		}

		public IoProcessorBuilder SetLogger(ILogger logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			return this;
		}

		public IoProcessorBuilder DisableVerboseValidation()
		{
			_verboseValidation = false;

			return this;
		}

		public IoProcessorBuilder SetSuspendIdlePeriod(TimeSpan suspendIdlePeriod)
		{
			if (suspendIdlePeriod <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(suspendIdlePeriod));

			_suspendIdlePeriod = suspendIdlePeriod;

			return this;
		}

		public IoProcessorBuilder AddResourceLoader(IResourceLoader resourceLoader)
		{
			if (resourceLoader == null) throw new ArgumentNullException(nameof(resourceLoader));

			(_resourceLoaders ??= ImmutableArray.CreateBuilder<IResourceLoader>()).Add(resourceLoader);

			return this;
		}

		public IoProcessorBuilder SetPersistence(PersistenceLevel persistenceLevel, IStorageProvider storageProvider)
		{
			if (!Enum.IsDefined(typeof(PersistenceLevel), persistenceLevel)) throw new InvalidEnumArgumentException(nameof(persistenceLevel), (int) persistenceLevel, typeof(PersistenceLevel));

			_persistenceLevel = persistenceLevel;
			_storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

			return this;
		}

		public IoProcessorBuilder AddEventProcessorFactory(IEventProcessorFactory eventProcessorFactory)
		{
			if (eventProcessorFactory == null) throw new ArgumentNullException(nameof(eventProcessorFactory));

			(_eventProcessorFactories ??= ImmutableArray.CreateBuilder<IEventProcessorFactory>()).Add(eventProcessorFactory);

			return this;
		}

		public IoProcessorBuilder AddServiceFactory(IServiceFactory serviceFactory)
		{
			if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

			(_serviceFactories ??= ImmutableArray.CreateBuilder<IServiceFactory>()).Add(serviceFactory);

			return this;
		}

		public IoProcessorBuilder AddDataModelHandlerFactory(IDataModelHandlerFactory dataModelHandlerFactory)
		{
			if (dataModelHandlerFactory == null) throw new ArgumentNullException(nameof(dataModelHandlerFactory));

			(_dataModelHandlerFactories ??= ImmutableArray.CreateBuilder<IDataModelHandlerFactory>()).Add(dataModelHandlerFactory);

			return this;
		}

		public IoProcessorBuilder AddCustomActionFactory(ICustomActionFactory customActionFactory)
		{
			if (customActionFactory == null) throw new ArgumentNullException(nameof(customActionFactory));

			(_customActionFactories ??= ImmutableArray.CreateBuilder<ICustomActionFactory>()).Add(customActionFactory);

			return this;
		}

		public IoProcessorBuilder SetConfigurationValue(string key, string value)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			(_configuration ??= ImmutableDictionary.CreateBuilder<string, string>())[key] = value ?? throw new ArgumentNullException(nameof(value));

			return this;
		}
	}
}