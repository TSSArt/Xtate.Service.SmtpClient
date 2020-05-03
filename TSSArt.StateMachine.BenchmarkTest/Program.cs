using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace TSSArt.StateMachine.BenchmarkTest
{
	public static class Program
	{
		private static void Main()
		{
			//var summary = BenchmarkRunner.Run<SimpleStateMachine>();

			var simpleStateMachine = new SimpleStateMachine();
			for (var i = 0; i < 50_000; i ++)
			{
				simpleStateMachine.InterpreterRunStateMachine();
			}
		}
	}

	internal class NoLogger : ILogger
	{
	#region Interface ILogger

		public ValueTask LogInfo(string sessionId, string? stateMachineName, string? label, DataModelValue data, CancellationToken token)                                                    => default;
		public ValueTask LogError(ErrorType errorType, string sessionId, string? stateMachineName, string? sourceEntityId, Exception exception, CancellationToken token)                     => default;
		public void      TraceProcessingEvent(string sessionId, EventType eventType, string name, string? sendId, string? invokeId, DataModelValue data, string? originType, string? origin) { }
		public void      TraceEnteringState(string sessionId, string stateId)                                                                                                                { }
		public void      TraceExitingState(string sessionId, string stateId)                                                                                                                 { }
		public void      TracePerformingTransition(string sessionId, string type, string? evt, string? target)                                                                               { }
		public bool      IsTracingEnabled                                                                                                                                                    => false;

	#endregion
	}

	[MemoryDiagnoser]
	[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 1, targetCount: 2)]
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
			_dataModelHandler = NoneDataModelHandler.Factory.CreateHandler(DefaultErrorProcessor.Instance);
			_host = new StateMachineHostBuilder().SetLogger(_logger).Build();
			_host.StartAsync().Wait();
		}

		[Benchmark]
		public void HostExecuteStateMachine()
		{
			var _ = _host.ExecuteAsync(_stateMachine).AsTask().Result;
		}

		[Benchmark]
		public void InterpreterRunStateMachine()
		{
			var options = new InterpreterOptions { Logger = _logger };
			var valueTask = StateMachineInterpreter.RunAsync(sessionId: "0", _stateMachine, _channelReader, options);
			var _ = valueTask.AsTask().Result;
		}

		[Benchmark]
		public void ModelBuilderCreate()
		{
			var _ = new InterpreterModelBuilder(_stateMachine, _dataModelHandler, ImmutableArray<ICustomActionFactory>.Empty, DefaultErrorProcessor.Instance);
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