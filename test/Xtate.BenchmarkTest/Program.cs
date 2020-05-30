using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Xtate.DataModel.XPath;

namespace Xtate.BenchmarkTest
{
	public static class Program
	{
		private static void Main()
		{
			var _ = BenchmarkRunner.Run<SimpleStateMachine>();
/*
			var simpleStateMachine = new SimpleStateMachine();
			
			simpleStateMachine.HostExecuteStateMachine();
			simpleStateMachine.HostExecuteStateMachine();
			simpleStateMachine.HostExecuteStateMachine();

				for (var i = 0; i < 50_000; i ++)
			{
				simpleStateMachine.HostExecuteStateMachine();
			}*/
		}
	}

	internal class NoLogger : ILogger
	{
	#region Interface ILogger

		public ValueTask ExecuteLog(ILoggerContext loggerContext, string? label, DataModelValue data, CancellationToken token) => default;

		public ValueTask LogError(ILoggerContext loggerContext, ErrorType errorType, Exception exception, string? sourceEntityId, CancellationToken token) => default;

		public void TraceProcessingEvent(ILoggerContext loggerContext, IEvent evt) { }

		public void TraceEnteringState(ILoggerContext loggerContext, IIdentifier stateId) { }

		public void TraceExitingState(ILoggerContext loggerContext, IIdentifier stateId) { }

		public void TracePerformingTransition(ILoggerContext loggerContext, TransitionType type, string? eventDescriptor, string? target) { }

		public void TraceInterpreterState(ILoggerContext loggerContext, StateMachineInterpreterState state) { }

		public bool IsTracingEnabled => false;

	#endregion
	}

	[MemoryDiagnoser]
	[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 2, targetCount: 5)]
	public class SimpleStateMachine
	{
		private readonly ChannelReader<IEvent> _channelReader;
		private readonly IDataModelHandler     _dataModelHandler;
		private readonly StateMachineHost      _host;
		private readonly ILogger               _logger = new NoLogger();
		private readonly IStateMachine         _stateMachine;

		public SimpleStateMachine()
		{
			_stateMachine = new StateMachineFluentBuilder(BuilderFactory.Instance).BeginFinal().SetId("1").EndFinal().Build();
			_channelReader = Channel.CreateUnbounded<IEvent>();
			_dataModelHandler = XPathDataModelHandler.Factory.CreateHandler(DefaultErrorProcessor.Instance);
			_host = new StateMachineHostBuilder().SetLogger(_logger).Build();
			_host.StartHostAsync().Wait();
		}

		[Benchmark]
		public void HostExecuteStateMachine()
		{
			var _ = _host.ExecuteStateMachineAsync(_stateMachine).AsTask().Result;
		}

		[Benchmark]
		public void InterpreterRunStateMachine()
		{
			var options = new InterpreterOptions { Logger = _logger };
			var valueTask = StateMachineInterpreter.RunAsync(SessionId.New(), _stateMachine, _channelReader, options);
			var _ = valueTask.AsTask().Result;
		}

		[Benchmark]
		public void ModelBuilderBuild()
		{
			var modelBuilder = new InterpreterModelBuilder(_stateMachine, _dataModelHandler, ImmutableArray<ICustomActionFactory>.Empty, DefaultErrorProcessor.Instance);
			var valueTask = modelBuilder.Build(ImmutableArray<IResourceLoader>.Empty, token: default);
			valueTask.AsTask().Wait();
		}
	}
}