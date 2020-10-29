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

		private bool IsVerbose => _logger.IsEnabled(LogEventLevel.Verbose);

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
					logger = logger.ForContext(propertyName: @"DataText", loggerContext.ConvertToText(data));

					if (string.IsNullOrWhiteSpace(label))
					{
						logger.Information(messageTemplate: @"(Data)", data.ToObject());
					}
					else
					{
						logger.Information(messageTemplate: @"{Label}: {Data}", label, data.ToObject());
					}

					break;

				case DataModelValueType.List:
					logger = logger.ForContext(propertyName: @"Data", data.ToObject(), destructureObjects: true)
								   .ForContext(propertyName: @"DataText", loggerContext.ConvertToText(data));

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
					Infrastructure.UnexpectedValue(data.Type);
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

			if (sourceEntityId is not null)
			{
				logger = logger.ForContext(propertyName: @"SourceEntityId", sourceEntityId);
			}

			logger.Error(exception, messageTemplate: @"{Message}", exception.Message);

			return default;
		}

		public ValueTask TraceProcessingEvent(ILoggerContext loggerContext, IEvent evt, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ProcessingEvent, IsVerbose))
									.ForContext(new EventEnricher(loggerContext, evt, IsVerbose));

				logger.Debug(@"Processing {EventType} event '{EventName}'");
			}

			return default;
		}

		public ValueTask TraceEnteringState(ILoggerContext loggerContext, IIdentifier stateId, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.EnteringState, IsVerbose));

				logger.Debug(messageTemplate: @"Entering state '{StateId}'", stateId.Value);
			}

			return default;
		}

		public ValueTask TraceEnteredState(ILoggerContext loggerContext, IIdentifier stateId, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.EnteredState, IsVerbose));

				logger.Debug(messageTemplate: @"Entered state '{StateId}'", stateId.Value);
			}

			return default;
		}

		public ValueTask TraceExitingState(ILoggerContext loggerContext, IIdentifier stateId, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ExitingState, IsVerbose));

				logger.Debug(messageTemplate: @"Exiting state '{StateId}'", stateId.Value);
			}

			return default;
		}

		public ValueTask TraceExitedState(ILoggerContext loggerContext, IIdentifier stateId, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId is null) throw new ArgumentNullException(nameof(stateId));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ExitedState, IsVerbose));

				logger.Debug(messageTemplate: @"Exited state '{StateId}'", stateId.Value);
			}

			return default;
		}

		public ValueTask TracePerformingTransition(ILoggerContext loggerContext, TransitionType type, string? eventDescriptor, string? target, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (IsTracingEnabled)
			{
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

			return default;
		}

		public ValueTask TracePerformedTransition(ILoggerContext loggerContext, TransitionType type, string? eventDescriptor, string? target, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (IsTracingEnabled)
			{
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

			return default;
		}

		public ValueTask TraceInterpreterState(ILoggerContext loggerContext, StateMachineInterpreterState state, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.InterpreterState, IsVerbose));

				logger.Debug(messageTemplate: @"Interpreter state has changed to '{InterpreterState}'", state);
			}

			return default;
		}

		public ValueTask TraceSendEvent(ILoggerContext loggerContext, IOutgoingEvent evt, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (evt is null) throw new ArgumentNullException(nameof(evt));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.InterpreterState, IsVerbose))
									.ForContext(new OutgoingEventEnricher(loggerContext, evt, IsVerbose));

				logger.Debug(messageTemplate: @"Send event '{EventName}'", EventName.ToName(evt.NameParts));
			}

			return default;
		}

		public ValueTask TraceCancelEvent(ILoggerContext loggerContext, SendId sendId, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (sendId is null) throw new ArgumentNullException(nameof(sendId));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.InterpreterState, IsVerbose));

				logger.Debug(messageTemplate: @"Cancel event '{SendId}'", sendId.Value);
			}

			return default;
		}

		public ValueTask TraceStartInvoke(ILoggerContext loggerContext, InvokeData invokeData, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (invokeData is null) throw new ArgumentNullException(nameof(invokeData));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.InterpreterState, IsVerbose))
									.ForContext(new InvokeEnricher(loggerContext, invokeData, IsVerbose));

				logger.Debug(messageTemplate: @"Start Invoke {InvokeId}", invokeData.InvokeId.Value);
			}

			return default;
		}

		public ValueTask TraceCancelInvoke(ILoggerContext loggerContext, InvokeId invokeId, CancellationToken token)
		{
			if (loggerContext is null) throw new ArgumentNullException(nameof(loggerContext));
			if (invokeId is null) throw new ArgumentNullException(nameof(invokeId));

			if (IsTracingEnabled)
			{
				var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.InterpreterState, IsVerbose));

				logger.Debug(messageTemplate: @"Start Invoke {InvokeId}", invokeId.Value);
			}

			return default;
		}

		public bool IsTracingEnabled => _logger.IsEnabled(LogEventLevel.Debug);

	#endregion

		private class LoggerEnricher : ILogEventEnricher
		{
			private readonly LogEventType   _logEventType;
			private readonly ILoggerContext _loggerContext;
			private readonly bool           _verboseLogging;

			public LoggerEnricher(ILoggerContext loggerContext, LogEventType logEventType, bool verboseLogging)
			{
				_loggerContext = loggerContext;
				_logEventType = logEventType;
				_verboseLogging = verboseLogging;
			}

		#region Interface ILogEventEnricher

			public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
			{
				if (_loggerContext.SessionId is { } sessionId)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"SessionId", sessionId.Value));
				}

				if (_loggerContext.StateMachineName is { } stateMachineName)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"StateMachineName", stateMachineName));
				}

				if (_logEventType != LogEventType.Undefined)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"LogEventType", _logEventType));
				}

				if (_verboseLogging && _loggerContext.GetDataModel() is { } dataModel)
				{
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"DataModel", dataModel, destructureObjects: true));
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"DataModelText", _loggerContext.ConvertToText(dataModel)));
				}

				if (_verboseLogging)
				{
					var activeStates = _loggerContext.GetActiveStates();
					if (!activeStates.IsDefault)
					{
						logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"ActiveStates", activeStates));
					}
				}
			}

		#endregion
		}

		private class EventEnricher : ILogEventEnricher
		{
			private readonly IEvent         _event;
			private readonly bool           _isVerbose;
			private readonly ILoggerContext _loggerContext;

			public EventEnricher(ILoggerContext loggerContext, IEvent evt, bool isVerbose)
			{
				_loggerContext = loggerContext;
				_event = evt;
				_isVerbose = isVerbose;
			}

		#region Interface ILogEventEnricher

			public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
			{
				if (!_event.NameParts.IsDefaultOrEmpty)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventName", EventName.ToName(_event.NameParts)));
				}

				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventType", _event.Type));

				if (_event.Origin is { } origin)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Origin", origin));
				}

				if (_event.OriginType is { } originType)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"OriginType", originType));
				}

				if (_event.SendId is { } sendId)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"SendId", sendId.Value));
				}

				if (_event.InvokeId is { } invokeId)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"InvokeId", invokeId.Value));
				}

				if (_isVerbose && !_event.Data.IsUndefined())
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Data", _event.Data.ToObject(), destructureObjects: true));
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"DataText", _loggerContext.ConvertToText(_event.Data)));
				}
			}

		#endregion
		}

		private class OutgoingEventEnricher : ILogEventEnricher
		{
			private readonly IOutgoingEvent _event;
			private readonly bool           _isVerbose;
			private readonly ILoggerContext _loggerContext;

			public OutgoingEventEnricher(ILoggerContext loggerContext, IOutgoingEvent evt, bool isVerbose)
			{
				_loggerContext = loggerContext;
				_event = evt;
				_isVerbose = isVerbose;
			}

		#region Interface ILogEventEnricher

			public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
			{
				if (!_event.NameParts.IsDefaultOrEmpty)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventName", EventName.ToName(_event.NameParts)));
				}

				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventType", _event.Type));

				if (_event.Target is { } target)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventTarget", target.ToString()));
				}

				if (_event.SendId is { } sendId)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"SendId", sendId.Value));
				}

				if (_event.DelayMs > 0)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Delay", _event.DelayMs));
				}

				if (_isVerbose && !_event.Data.IsUndefined())
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Data", _event.Data.ToObject(), destructureObjects: true));
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"DataText", _loggerContext.ConvertToText(_event.Data)));
				}
			}

		#endregion
		}

		private class InvokeEnricher : ILogEventEnricher
		{
			private readonly InvokeData     _invokeData;
			private readonly bool           _isVerbose;
			private readonly ILoggerContext _loggerContext;

			public InvokeEnricher(ILoggerContext loggerContext, InvokeData invokeData, bool isVerbose)
			{
				_loggerContext = loggerContext;
				_invokeData = invokeData;
				_isVerbose = isVerbose;
			}

		#region Interface ILogEventEnricher

			public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
			{
				if (_invokeData.InvokeId is { } invokeId)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"InvokeId", invokeId.Value));
				}

				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"InvokeType", _invokeData.Type));

				if (_invokeData.Source is { } source)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"InvokeSource", source));
				}

				if (_isVerbose)
				{
					if (_invokeData.RawContent is { } rawContent)
					{
						logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"RawContent", rawContent));
					}

					if (!_invokeData.Content.IsUndefined())
					{
						logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"Content", _invokeData.Content, destructureObjects: true));
						logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"ContentText", _loggerContext.ConvertToText(_invokeData.Content)));
					}

					if (!_invokeData.Parameters.IsUndefined())
					{
						logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"Parameters", _invokeData.Parameters, destructureObjects: true));
						logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"ParametersText", _loggerContext.ConvertToText(_invokeData.Parameters)));
					}
				}
			}

		#endregion
		}
	}
}