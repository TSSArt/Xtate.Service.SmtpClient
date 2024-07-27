// Copyright © 2019-2024 Sergii Artemenko
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xtate.Core;

namespace Xtate;

public class SerilogLogWriterConfiguration
{
	public SerilogLogWriterConfiguration(Action<LoggerConfiguration> options)
	{
		Value = new LoggerConfiguration();

		options(Value);
	}

	public LoggerConfiguration Value { get; }
}

public class SerilogLogWriter<TSource>(SerilogLogWriterConfiguration configuration) : ILogWriter<TSource>, IDisposable
{
	private readonly Logger _logger =
		configuration
			.Value
			.Destructure.With<DestructuringPolicy>()
			.CreateLogger();

#region Interface IDisposable

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

#endregion

#region Interface ILogWriter<TSource>

	public bool IsEnabled(Level level) => _logger.IsEnabled(GetLogEventLevel(level));

	public async ValueTask Write(Level level,
								 int eventId,
								 string? message,
								 IAsyncEnumerable<LoggingParameter>? parameters)
	{
		List<LoggingParameter>? prms = default;
		Exception? exception = default;

		if (parameters is not null)
		{
			await foreach (var prm in parameters.ConfigureAwait(false))
			{
				if (exception is null && prm is { Name: @"Exception", Value: Exception ex })
				{
					exception = ex;
				}
				else
				{
					prms ??= [];
					prms.Add(prm);
				}
			}
		}

		var logger = _logger.ForContext(typeof(TSource));

		if (prms is not null)
		{
			logger = logger.ForContext(new ParametersLogEventEnricher(prms));
		}

		logger.Write(GetLogEventLevel(level), exception, message ?? string.Empty);
	}

#endregion

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_logger.Dispose();
		}
	}

	private static LogEventLevel GetLogEventLevel(Level level) =>
		level switch
		{
			Level.Info    => LogEventLevel.Information,
			Level.Warning => LogEventLevel.Warning,
			Level.Error   => LogEventLevel.Error,
			Level.Debug   => LogEventLevel.Debug,
			Level.Trace   => LogEventLevel.Verbose,
			Level.Verbose => LogEventLevel.Verbose,
			_             => Infra.Unexpected<LogEventLevel>(level)
		};

	private class ParametersLogEventEnricher(IEnumerable<LoggingParameter> parameters) : ILogEventEnricher
	{
	#region Interface ILogEventEnricher

		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			foreach (var parameter in parameters)
			{
				var name = string.IsNullOrEmpty(parameter.Namespace) ? parameter.Name : parameter.Namespace + @"_" + parameter.Name;

				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name, parameter.Value, destructureObjects: true));
			}
		}

	#endregion
	}

	/*
	private bool IsVerbose => _logger.IsEnabled(LogEventLevel.Verbose);

#region Interface ILogger

	public async ValueTask ExecuteLogOld(LogLevel logLevel,
										 string? message,
										 DataModelValue arguments,
										 Exception? exception) =>
		throw new NotImplementedException();

	public async ValueTask LogErrorOld(ErrorType errorType, Exception exception, string? sourceEntityId) => throw new NotImplementedException();

	public ValueTask ExecuteLog(LogLevel logLevel,
								string? message,
								DataModelValue data,
								Exception? exception)
	{
		var logEventLevel = logLevel switch
							{
								LogLevel.Info    => LogEventLevel.Information,
								LogLevel.Warning => LogEventLevel.Warning,
								LogLevel.Error   => LogEventLevel.Error,
								_                => Infra.Unexpected<LogEventLevel>(logLevel)
							};

		if (!_logger.IsEnabled(logEventLevel))
		{
			return default;
		}

		var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.ExecuteLog, IsVerbose));

		switch (data.Type)
		{
			case DataModelValueType.Undefined:
			case DataModelValueType.Null:
			case DataModelValueType.String when string.IsNullOrWhiteSpace(data.AsString()):
				if (string.IsNullOrWhiteSpace(message))
				{
					logger.Write(logEventLevel, messageTemplate: @"(empty)", exception);
				}
				else
				{
					logger.Write(logEventLevel, messageTemplate: @"{Label}", message, exception);
				}

				break;

			case DataModelValueType.Number:
			case DataModelValueType.DateTime:
			case DataModelValueType.Boolean:
			case DataModelValueType.String:
				logger = logger.ForContext(propertyName: @"DataText", ConvertToText(_loggerContext, data));

				if (string.IsNullOrWhiteSpace(message))
				{
					logger.Write(logEventLevel, messageTemplate: @"(Data)", data.ToObject(), exception);
				}
				else
				{
					logger.Write(logEventLevel, messageTemplate: @"{Label}: {Data}", message, data.ToObject(), exception);
				}

				break;

			case DataModelValueType.List:
				logger = logger.ForContext(propertyName: @"Data", data.ToObject(), destructureObjects: true)
							   .ForContext(propertyName: @"DataText", ConvertToText(_loggerContext, data));

				if (string.IsNullOrWhiteSpace(message))
				{
					logger.Write(logEventLevel, messageTemplate: @"(data)", exception);
				}
				else
				{
					logger.Write(logEventLevel, messageTemplate: @"{Label}: (data)", message, exception);
				}

				break;

			default:
				Infra.Unexpected(data.Type);
				break;
		}

		return default;
	}

	public ValueTask LogError(ErrorType errorType,
							  Exception exception,
							  string? sourceEntityId)
	{
		if (exception is null) throw new ArgumentNullException(nameof(exception));

		if (!_logger.IsEnabled(LogEventLevel.Error))
		{
			return default;
		}

		var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.Error, IsVerbose))
							.ForContext(propertyName: @"ErrorType", errorType);

		if (sourceEntityId is not null)
		{
			logger = logger.ForContext(propertyName: @"SourceEntityId", sourceEntityId);
		}

		logger.Error(exception, messageTemplate: @"{Message}", exception.Message);

		return default;
	}

	public ValueTask TraceProcessingEvent(IEvent evt)
	{
		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.ProcessingEvent, IsVerbose))
								.ForContext(new EventEnricher(_loggerContext, evt, IsVerbose));

			logger.Debug(@"Processing {EventType} event '{EventName}'");
		}

		return default;
	}

	public ValueTask TraceEnteringState(IIdentifier stateId)
	{
		if (stateId is null) throw new ArgumentNullException(nameof(stateId));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.EnteringState, IsVerbose));

			logger.Debug(messageTemplate: @"Entering state '{StateId}'", stateId.Value);
		}

		return default;
	}

	public ValueTask TraceEnteredState(IIdentifier stateId)
	{
		if (stateId is null) throw new ArgumentNullException(nameof(stateId));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.EnteredState, IsVerbose));

			logger.Debug(messageTemplate: @"Entered state '{StateId}'", stateId.Value);
		}

		return default;
	}

	public ValueTask TraceExitingState(IIdentifier stateId)
	{
		if (stateId is null) throw new ArgumentNullException(nameof(stateId));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.ExitingState, IsVerbose));

			logger.Debug(messageTemplate: @"Exiting state '{StateId}'", stateId.Value);
		}

		return default;
	}

	public ValueTask TraceExitedState(IIdentifier stateId)
	{
		if (stateId is null) throw new ArgumentNullException(nameof(stateId));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.ExitedState, IsVerbose));

			logger.Debug(messageTemplate: @"Exited state '{StateId}'", stateId.Value);
		}

		return default;
	}

	public ValueTask TracePerformingTransition(TransitionType type,
											   string? eventDescriptor,
											   string? target)
	{
		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.PerformingTransition, IsVerbose));

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

	public ValueTask TracePerformedTransition(TransitionType type,
											  string? eventDescriptor,
											  string? target)
	{
		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.PerformedTransition, IsVerbose));

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

	public ValueTask TraceInterpreterState(StateMachineInterpreterState state)
	{
		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.InterpreterState, IsVerbose));

			logger.Debug(messageTemplate: @"Interpreter state has changed to '{InterpreterState}'", state);
		}

		return default;
	}

	public ValueTask TraceSendEvent(IOutgoingEvent outgoingEvent)
	{
		if (outgoingEvent is null) throw new ArgumentNullException(nameof(outgoingEvent));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.InterpreterState, IsVerbose))
								.ForContext(new OutgoingEventEnricher(_loggerContext, outgoingEvent, IsVerbose));

			logger.Debug(messageTemplate: @"Send event '{EventName}'", EventName.ToName(outgoingEvent.NameParts));
		}

		return default;
	}

	public ValueTask TraceCancelEvent(SendId sendId)
	{
		if (sendId is null) throw new ArgumentNullException(nameof(sendId));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.InterpreterState, IsVerbose));

			logger.Debug(messageTemplate: @"Cancel event '{SendId}'", sendId.Value);
		}

		return default;
	}

	public ValueTask TraceStartInvoke(InvokeData invokeData)
	{
		if (invokeData is null) throw new ArgumentNullException(nameof(invokeData));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.InterpreterState, IsVerbose))
								.ForContext(new InvokeEnricher(_loggerContext, invokeData, IsVerbose));

			logger.Debug(messageTemplate: @"Start Invoke {InvokeId}", invokeData.InvokeId.Value);
		}

		return default;
	}

	public ValueTask TraceCancelInvoke(InvokeId invokeId)
	{
		if (invokeId is null) throw new ArgumentNullException(nameof(invokeId));

		if (IsTracingEnabled)
		{
			var logger = _logger.ForContext(new LoggerEnricher(_loggerContext, LogEventType.InterpreterState, IsVerbose));

			logger.Debug(messageTemplate: @"Cancel Invoke {InvokeId}", invokeId.Value);
		}

		return default;
	}

	public bool IsTracingEnabled => _logger.IsEnabled(LogEventLevel.Debug);

#endregion

	private static string ConvertToText(ILoggerContext? loggerContext, in DataModelValue value)
	{
		if (loggerContext is IInterpreterLoggerContext interpreterLoggerContext)
		{
			return interpreterLoggerContext.ConvertToText(value);
		}

		return value.ToString(CultureInfo.InvariantCulture);
	}

	private class LoggerEnricher : ILogEventEnricher
	{
		private readonly LogEventType    _logEventType;
		private readonly ILoggerContext? _loggerContext;
		private readonly bool            _verboseLogging;

		public LoggerEnricher(ILoggerContext? loggerContext, LogEventType logEventType, bool verboseLogging)
		{
			_loggerContext = loggerContext;
			_logEventType = logEventType;
			_verboseLogging = verboseLogging;
		}

	#region Interface ILogEventEnricher

		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (_loggerContext is IInterpreterLoggerContext interpreterLoggerContext)
			{
				Enrich(interpreterLoggerContext, logEvent, propertyFactory);
			}

			if (_loggerContext?.GetProperties() is { Count: > 0 } properties)
			{
				foreach (var pair in properties.KeyValuePairs)
				{
					logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(pair.Key, pair.Value, destructureObjects: true));
				}
			}
		}

	#endregion

		private void Enrich(IInterpreterLoggerContext interpreterLoggerContext, LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (interpreterLoggerContext.SessionId is { } sessionId)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"SessionId", sessionId.Value));
			}

			if (interpreterLoggerContext.StateMachine.Name is { } stateMachineName)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"StateMachineName", stateMachineName));
			}

			if (_logEventType != LogEventType.Undefined)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"LogEventType", _logEventType));
			}

			if (_verboseLogging && interpreterLoggerContext.GetDataModel() is { } dataModel)
			{
				logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"DataModel", dataModel, destructureObjects: true));
				logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"DataModelText", interpreterLoggerContext.ConvertToText(dataModel)));
			}

			if (_verboseLogging)
			{
				var activeStates = interpreterLoggerContext.GetActiveStates();
				if (!activeStates.IsDefault)
				{
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"ActiveStates", activeStates));
				}
			}
		}
	}

	private class EventEnricher : ILogEventEnricher
	{
		private readonly IEvent          _event;
		private readonly bool            _isVerbose;
		private readonly ILoggerContext? _loggerContext;

		public EventEnricher(ILoggerContext? loggerContext, IEvent evt, bool isVerbose)
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
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"DataText", ConvertToText(_loggerContext, _event.Data)));
			}
		}

	#endregion
	}

	private class OutgoingEventEnricher : ILogEventEnricher
	{
		private readonly bool            _isVerbose;
		private readonly ILoggerContext? _loggerContext;
		private readonly IOutgoingEvent  _outgoingEvent;

		public OutgoingEventEnricher(ILoggerContext? loggerContext, IOutgoingEvent outgoingEvent, bool isVerbose)
		{
			_loggerContext = loggerContext;
			_outgoingEvent = outgoingEvent;
			_isVerbose = isVerbose;
		}

	#region Interface ILogEventEnricher

		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (!_outgoingEvent.NameParts.IsDefaultOrEmpty)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventName", EventName.ToName(_outgoingEvent.NameParts)));
			}

			logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventType", _outgoingEvent.Type));

			if (_outgoingEvent.Target is { } target)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"EventTarget", target.ToString()));
			}

			if (_outgoingEvent.SendId is { } sendId)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"SendId", sendId.Value));
			}

			if (_outgoingEvent.DelayMs > 0)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Delay", _outgoingEvent.DelayMs));
			}

			if (_isVerbose && !_outgoingEvent.Data.IsUndefined())
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"Data", _outgoingEvent.Data.ToObject(), destructureObjects: true));
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name: @"DataText", ConvertToText(_loggerContext, _outgoingEvent.Data)));
			}
		}

	#endregion
	}

	private class InvokeEnricher : ILogEventEnricher
	{
		private readonly InvokeData      _invokeData;
		private readonly bool            _isVerbose;
		private readonly ILoggerContext? _loggerContext;

		public InvokeEnricher(ILoggerContext? loggerContext, InvokeData invokeData, bool isVerbose)
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
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"ContentText", ConvertToText(_loggerContext, _invokeData.Content)));
				}

				if (!_invokeData.Parameters.IsUndefined())
				{
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"Parameters", _invokeData.Parameters, destructureObjects: true));
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name: @"ParametersText", ConvertToText(_loggerContext, _invokeData.Parameters)));
				}
			}
		}

	#endregion
	}
	*/
}