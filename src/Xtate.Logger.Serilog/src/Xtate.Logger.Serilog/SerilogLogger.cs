#region Copyright © 2019-2020 Sergii Artemenko
// 
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
// 
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
			ExitingState,
			PerformingTransition,
			InterpreterState
		}

		private readonly Logger _logger;

		public SerilogLogger(LoggerConfiguration configuration)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));

			_logger = configuration.CreateLogger();
		}

	#region Interface ILogger

		public ValueTask ExecuteLog(ILoggerContext loggerContext, string? label, DataModelValue data, CancellationToken token)
		{
			if (loggerContext == null) throw new ArgumentNullException(nameof(loggerContext));

			if (!_logger.IsEnabled(LogEventLevel.Information))
			{
				return default;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ExecuteLog));

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
			if (loggerContext == null) throw new ArgumentNullException(nameof(loggerContext));

			if (exception == null) throw new ArgumentNullException(nameof(exception));

			if (!_logger.IsEnabled(LogEventLevel.Error))
			{
				return default;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.Error))
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
			if (loggerContext == null) throw new ArgumentNullException(nameof(loggerContext));

			if (!_logger.IsEnabled(LogEventLevel.Verbose))
			{
				return;
			}

			var logger = _logger.ForContext(new ILogEventEnricher[] { new LoggerEnricher(loggerContext, LogEventType.ProcessingEvent), new EventEnricher(evt) });

			logger.Verbose(@"Processing {EventType} event '{EventName}'");
		}

		public void TraceEnteringState(ILoggerContext loggerContext, IIdentifier stateId)
		{
			if (loggerContext == null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId == null) throw new ArgumentNullException(nameof(stateId));

			if (!_logger.IsEnabled(LogEventLevel.Verbose))
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.EnteringState));

			logger.Verbose(messageTemplate: @"Entering state '{StateId}'", stateId.Value);
		}

		public void TraceExitingState(ILoggerContext loggerContext, IIdentifier stateId)
		{
			if (loggerContext == null) throw new ArgumentNullException(nameof(loggerContext));
			if (stateId == null) throw new ArgumentNullException(nameof(stateId));

			if (!_logger.IsEnabled(LogEventLevel.Verbose))
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.ExitingState));

			logger.Verbose(messageTemplate: @"Exiting state '{StateId}'", stateId.Value);
		}

		public void TracePerformingTransition(ILoggerContext loggerContext, TransitionType type, string? eventDescriptor, string? target)
		{
			if (loggerContext == null) throw new ArgumentNullException(nameof(loggerContext));

			if (!_logger.IsEnabled(LogEventLevel.Verbose))
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.PerformingTransition));

			if (eventDescriptor == null)
			{
				logger.Verbose(messageTemplate: @"Eventless {TransitionType} transition to '{Target}'", target);
			}
			else
			{
				logger.Verbose(messageTemplate: @"{TransitionType} transition to '{Target}'. Event descriptor '{EventDescriptor}'", target, eventDescriptor);
			}
		}

		public void TraceInterpreterState(ILoggerContext loggerContext, StateMachineInterpreterState state)
		{
			if (loggerContext == null) throw new ArgumentNullException(nameof(loggerContext));

			if (!_logger.IsEnabled(LogEventLevel.Verbose))
			{
				return;
			}

			var logger = _logger.ForContext(new LoggerEnricher(loggerContext, LogEventType.InterpreterState));

			logger.Verbose(messageTemplate: @"Interpreter state has changed to '{InterpreterState}'", state);
		}

		public bool IsTracingEnabled => _logger.IsEnabled(LogEventLevel.Verbose);

	#endregion

		private class LoggerEnricher : ILogEventEnricher
		{
			private readonly LogEventType   _logEventType;
			private readonly ILoggerContext _loggerContext;

			public LoggerEnricher(ILoggerContext loggerContext, LogEventType logEventType)
			{
				_loggerContext = loggerContext;
				_logEventType = logEventType;
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