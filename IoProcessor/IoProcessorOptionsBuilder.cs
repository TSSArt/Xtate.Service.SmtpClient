using System;
using System.Collections.Immutable;
using System.ComponentModel;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class IoProcessorOptionsBuilder
	{
		private ImmutableArray<IEventProcessorFactory>.Builder?   _eventProcessorFactories;
		private ImmutableArray<IServiceFactory>.Builder?          _serviceFactories;
		private ImmutableArray<IDataModelHandlerFactory>.Builder? _dataModelHandlerFactories;
		private ImmutableArray<ICustomActionFactory>.Builder?     _customActionFactories;
		private ImmutableDictionary<string, string>.Builder?      _configuration;
		private ILogger?                                          _logger;
		private PersistenceLevel                                  _persistenceLevel;
		private IStorageProvider?                                 _storageProvider;
		private IResourceLoader?                                  _resourceLoader;
		private TimeSpan                                          _suspendIdlePeriod;
		private bool                                              _verboseValidation = true;

		public IoProcessorOptions Build()
		{
			return new IoProcessorOptions
				   {
						   EventProcessorFactories = _eventProcessorFactories?.ToImmutable() ?? default,
						   ServiceFactories = _serviceFactories?.ToImmutable() ?? default,
						   DataModelHandlerFactories = _dataModelHandlerFactories?.ToImmutable() ?? default,
						   CustomActionFactories = _customActionFactories?.ToImmutable() ?? default,
						   Configuration = _configuration?.ToImmutable(),
						   Logger = _logger,
						   PersistenceLevel = _persistenceLevel,
						   StorageProvider = _storageProvider,
						   ResourceLoader = _resourceLoader,
						   SuspendIdlePeriod = _suspendIdlePeriod,
						   VerboseValidation = _verboseValidation
				   };
		}

		public IoProcessorOptionsBuilder SetLogger(ILogger logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			return this;
		}

		public IoProcessorOptionsBuilder DisableVerboseValidation()
		{
			_verboseValidation = false;

			return this;
		}

		public IoProcessorOptionsBuilder SetSuspendIdlePeriod(TimeSpan suspendIdlePeriod)
		{
			if (suspendIdlePeriod <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(suspendIdlePeriod));

			_suspendIdlePeriod = suspendIdlePeriod;

			return this;
		}

		public IoProcessorOptionsBuilder SetResourceLoader(IResourceLoader resourceLoader)
		{
			_resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));

			return this;
		}

		public IoProcessorOptionsBuilder SetPersistence(PersistenceLevel persistenceLevel, IStorageProvider storageProvider)
		{
			if (!Enum.IsDefined(typeof(PersistenceLevel), persistenceLevel)) throw new InvalidEnumArgumentException(nameof(persistenceLevel), (int) persistenceLevel, typeof(PersistenceLevel));

			_persistenceLevel = persistenceLevel;
			_storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

			return this;
		}

		public IoProcessorOptionsBuilder AddEventProcessorFactory(IEventProcessorFactory eventProcessorFactory)
		{
			if (eventProcessorFactory == null) throw new ArgumentNullException(nameof(eventProcessorFactory));

			(_eventProcessorFactories ??= ImmutableArray.CreateBuilder<IEventProcessorFactory>()).Add(eventProcessorFactory);

			return this;
		}

		public IoProcessorOptionsBuilder AddServiceFactory(IServiceFactory serviceFactory)
		{
			if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

			(_serviceFactories ??= ImmutableArray.CreateBuilder<IServiceFactory>()).Add(serviceFactory);

			return this;
		}

		public IoProcessorOptionsBuilder AddDataModelHandlerFactory(IDataModelHandlerFactory dataModelHandlerFactory)
		{
			if (dataModelHandlerFactory == null) throw new ArgumentNullException(nameof(dataModelHandlerFactory));

			(_dataModelHandlerFactories ??= ImmutableArray.CreateBuilder<IDataModelHandlerFactory>()).Add(dataModelHandlerFactory);

			return this;
		}

		public IoProcessorOptionsBuilder AddCustomActionFactory(ICustomActionFactory customActionFactory)
		{
			if (customActionFactory == null) throw new ArgumentNullException(nameof(customActionFactory));

			(_customActionFactories ??= ImmutableArray.CreateBuilder<ICustomActionFactory>()).Add(customActionFactory);

			return this;
		}

		public IoProcessorOptionsBuilder SetConfigurationValue(string key, string value)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			(_configuration ??= ImmutableDictionary.CreateBuilder<string, string>())[key] = value ?? throw new ArgumentNullException(nameof(value));

			return this;
		}
	}
}