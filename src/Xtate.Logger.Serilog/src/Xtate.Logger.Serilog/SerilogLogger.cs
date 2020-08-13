#region Copyright © 2019-2020 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public class SerilogLogger : ILogger
	{
		private class DataModelListDestructuringPolicy : IDestructuringPolicy
		{
			public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
			{
				if (!(value is DataModelList list))
				{
					result = default;

					return false;
				}

				result = GetLogEventPropertyValue(list);

				return true;
			}

			private static LogEventPropertyValue GetLogEventPropertyValue(in DataModelValue value) =>
					value.Type switch
					{
							DataModelValueType.Undefined => new ScalarValue(value.ToObject()),
							DataModelValueType.Null => new ScalarValue(value.ToObject()),
							DataModelValueType.String => new ScalarValue(value.ToObject()),
							DataModelValueType.Number => new ScalarValue(value.ToObject()),
							DataModelValueType.DateTime => new ScalarValue(value.ToObject()),
							DataModelValueType.Boolean => new ScalarValue(value.ToObject()),
							DataModelValueType.Array => GetLogEventPropertyValue(value.AsList()),
							DataModelValueType.Object => GetLogEventPropertyValue(value.AsList()),
							_ => Infrastructure.UnexpectedValue<LogEventPropertyValue>()
					};

			private static LogEventPropertyValue GetLogEventPropertyValue(DataModelList list)
			{
				var index = 0;
				foreach (var entry in list.Entries)
				{
					if (index ++ != entry.Index)
					{
						return new StructureValue(EnumerateEntries(true));
					}
				}

				if (list.GetMetadata() is { })
				{
					return new StructureValue(EnumerateEntries(false));
				}

				foreach (var entry in list.Entries)
				{
					if (entry.Key is { } || entry.Metadata is { })
					{
						return new StructureValue(EnumerateEntries(false));
					}
				}

				if (list.Count == 0 && list is DataModelObject)
				{
					return new StructureValue(Array.Empty<LogEventProperty>());
				}

				return new SequenceValue(EnumerateValues());

				IEnumerable<LogEventProperty> EnumerateEntries(bool showIndex)
				{
					foreach (var entry in list.Entries)
					{
						var name = GetName(entry.Key);
						yield return new LogEventProperty(name, GetLogEventPropertyValue(entry.Value));
						
						if (showIndex)
						{
							yield return new LogEventProperty(name + @":(index)", new ScalarValue(entry.Index));
						}

						if (entry.Metadata is { } entryMetadata)
						{
							yield return new LogEventProperty(name + @":(meta)", GetLogEventPropertyValue(entryMetadata));
						}
					}

					if (list.GetMetadata() is { } metadata)
					{
						yield return new LogEventProperty(name: @"(meta)", GetLogEventPropertyValue(metadata));
					}
				}

				IEnumerable<LogEventPropertyValue> EnumerateValues()
				{
					foreach (var value in list.Values)
					{
						yield return GetLogEventPropertyValue(value);
					}
				}
			}

			private static string GetName(string? key)
			{
				if (key is null)
				{
					return "(null)";
				}

				if (string.IsNullOrWhiteSpace(key))
				{
					return "(" + key + ")";
				}

				return key;
			}
		}

		public enum LogEventType
		{
			Undefined,
			ExecuteLog,
			Error,
			ProcessingEvent,
			EnteringState,
			EnteredState,
			ExitingState,
			ExitedState,
			PerformingTransition,
			PerformedTransition,
			InterpreterState
		}

		private readonly Logger _logger;

		public SerilogLogger(LoggerConfiguration configuration)
		{
			if (configuration is null) throw new ArgumentNullException(nameof(configuration));

			_logger = configuration.Destructure.With<DataModelListDestructuringPolicy>().CreateLogger();
		}

	#region Interface ILogger

		public ValueTask ExecuteLog(ILoggerContext loggerContext, string? label, DataModelValue data, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (!_logger.IsEnabled(LogEventLevel.Information))
			{
				return default;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ExecuteLog, IsVerbose));

			switch (data.Type)
			{
				case DataModelValueType.Undefined:
				case DataModelValueType.Null:
				case DataModelValueType.String when string.IsNullOrWhiteSpace(data.AsString()):
					if (string.IsNullOrWhiteSpace(label))
					{
						logger.Information(messageTemplate: @"(empty)");
					}
					else
					{
						logger.Information(messageTemplate: @"{Label}", label);
					}

					break;

				case DataModelValueType.Number:
				case DataModelValueType.DateTime:
				case DataModelValueType.Boolean:
				case DataModelValueType.String:
					if (string.IsNullOrWhiteSpace(label))
					{
						logger.Information(messageTemplate: @"(Data)", data.ToObject());
					}
					else
					{
						logger.Information(messageTemplate: @"{Label}: {Data}", label, data.ToObject());
					}

					break;

				case DataModelValueType.Object:
				case DataModelValueType.Array:
					logger = logger.ForContext(propertyName: @"Data", data.ToObject(), destructureObjects: true);
					if (string.IsNullOrWhiteSpace(label))
					{
						logger.Information(messageTemplate: @"(data)");
					}
					else
					{
						logger.Information(messageTemplate: @"{Label}: (data)", label);
					}

					break;

				default:
					Infrastructure.UnexpectedValue();
					break;
			}

			return default;
		}

		public ValueTask LogError(ILoggerContext loggerContext, ErrorType errorType, Exception exception, string? sourceEntityId, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (exception is null) throw new ArgumentNullException(nameof(exception));

			if (!_logger.IsEnabled(LogEventLevel.Error))
			{
				return default;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.Error, IsVerbose))
								.ForContext(propertyName: @"ErrorType", errorType);

			if (sourceEntityId != null)
			{
				logger = logger.ForContext(propertyName: @"SourceEntityId", sourceEntityId);
			}

			logger.Error(exception, messageTemplate: @"{Message}", exception.Message);

			return default;
		}

		public void TraceProcessingEvent(ILoggerContext loggerContext, IEvent evt)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new ILogEventEnricher[] { new LoggerEnricher(loggerContext, LogEventType.ProcessingEvent, IsVerbose), new EventEnricher(evt) });

			logger.Debug(@"Processing {EventType} event '{EventName}'");
		}

		public void TraceEnteringState(ILoggerContext loggerContext, IIdentifier stateId)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.EnteringState, IsVerbose));

			logger.Debug(messageTemplate: @"Entering state '{StateId}'", stateId.Value);
		}

		public void TraceEnteredState(ILoggerContext loggerContext, IIdentifier stateId)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.EnteredState, IsVerbose));

			logger.Debug(messageTemplate: @"Entered state '{StateId}'", stateId.Value);
		}

		public void TraceExitingState(ILoggerContext loggerContext, IIdentifier stateId)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ExitingState, IsVerbose));

			logger.Debug(messageTemplate: @"Exiting state '{StateId}'", stateId.Value);
		}

		public void TraceExitedState(ILoggerContext loggerContext, IIdentifier stateId)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ExitedState, IsVerbose));

			logger.Debug(messageTemplate: @"Exited state '{StateId}'", stateId.Value);
		}

		public void TracePerformingTransition(ILoggerContext loggerContext, TransitionType type, string? eventDescriptor, string? target)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.PerformingTransition, IsVerbose));

			if (eventDescriptor is null)
			{
				logger.Debug(messageTemplate: @"Performing eventless {TransitionType} transition to '{Target}'", target);
			}
			else
			{
				logger.Debug(messageTemplate: @"Performing {TransitionType} transition to '{Target}'. Event descriptor '{EventDescriptor}'", target, eventDescriptor);
			}
		}

		public void TracePerformedTransition(ILoggerContext loggerContext, TransitionType type, string? eventDescriptor, string? target)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.PerformedTransition, IsVerbose));

			if (eventDescriptor is null)
			{
				logger.Debug(messageTemplate: @"Performed eventless {TransitionType} transition to '{Target}'", target);
			}
			else
			{
				logger.Debug(messageTemplate: @"Performed {TransitionType} transition to '{Target}'. Event descriptor '{EventDescriptor}'", target, eventDescriptor);
			}
		}

		public void TraceInterpreterState(ILoggerContext loggerContext, StateMachineInterpreterState state)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (!IsTracingEnabled)
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.InterpreterState, IsVerbose));

			logger.Debug(messageTemplate: @"Interpreter state has changed to '{InterpreterState}'", state);
		}

		public bool IsTracingEnabled => _logger.IsEnabled(LogEventLevel.Debug);
		
		private bool IsVerbose => _logger.IsEnabled(LogEventLevel.Verbose);

	#endregion

		private class LoggerEnricher : ILogEventEnricher
		{
			private readonly LogEventType   _logEventType;
			private readonly bool           _verboseLogging;
			private readonly ILoggerContext _loggerContext;

			public LoggerEnricher(ILoggerContext loggerContext, LogEventType logEventType, bool verboseLogging)
			{
				_loggerContext = loggerContext;
				_logEventType = logEventType;
				_verboseLogging = verboseLogging;
			}

		#region Interface ILogEventEnricher

			public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
			{
				if (_loggerContext.SessionId != null)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"SessionId", _loggerContext.SessionId.Value));
				}

				if (_loggerContext.StateMachineName != null)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"StateMachineName", _loggerContext.StateMachineName));
				}

				if (_logEventType != LogEventType.Undefined)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"LogEventType", _logEventType));
				}

				if (_verboseLogging && _loggerContext.GetDataModel() is { } dataModel)
				{
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"DataModel", dataModel, destructureObjects: true));
				}
			}

		#endregion
		}

		private class EventEnricher : ILogEventEnricher
		{
			private readonly IEvent _event;

			public EventEnricher(IEvent evt) => _event = evt;

		#region Interface ILogEventEnricher

			public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
			{
				if (!_event.NameParts.IsDefaultOrEmpty)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventName", EventName.ToName(_event.NameParts)));
				}

				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventType", _event.Type));

				if (_event.Origin != null)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Origin", _event.Origin));
				}

				if (_event.OriginType != null)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"OriginType", _event.OriginType));
				}

				if (_event.SendId != null)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"SendId", _event.SendId.Value));
				}

				if (_event.InvokeId != null)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"InvokeId", _event.InvokeId.Value));
				}

				if (!_event.Data.IsUndefined())
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Data", _event.Data.ToObject(), destructureObjects: true));
				}
			}

		#endregion
		}
	}
}