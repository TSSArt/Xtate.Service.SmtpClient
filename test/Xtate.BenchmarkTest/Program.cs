#region Copyright © 2019-2021 Sergii Artemenko

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
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Xtate.Builder;
using Xtate.Core;
using Xtate.DataModel;
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

	internal class NoLogger : ILoggerOld
	{
	#region Interface ILogger

		public ValueTask ExecuteLog(LogLevel logLevel,
									string? message,
									DataModelValue arguments,
									Exception? exception) =>
			default;

		public ValueTask LogError(ErrorType errorType,
								  Exception exception,
								  string? sourceEntityId) =>
			default;

		public ValueTask TraceProcessingEvent(IEvent evt) => default;

		public ValueTask TraceEnteringState(IIdentifier stateId) => default;

		public ValueTask TraceEnteredState(IIdentifier stateId) => default;

		public ValueTask TraceExitingState(IIdentifier stateId) => default;

		public ValueTask TraceExitedState(IIdentifier stateId) => default;

		public async ValueTask ExecuteLogOld(LogLevel logLevel,
											 string? message,
											 DataModelValue arguments,
											 Exception? exception) =>
			throw new NotImplementedException();

		public async ValueTask LogErrorOld(ErrorType errorType, Exception exception, string? sourceEntityId) => throw new NotImplementedException();

		public ValueTask TracePerformingTransition(TransitionType type,
												   string? eventDescriptor,
												   string? target) =>
			default;

		public ValueTask TracePerformedTransition(TransitionType type,
												  string? eventDescriptor,
												  string? target) =>
			default;

		public ValueTask TraceInterpreterState(StateMachineInterpreterState state) => default;

		public ValueTask TraceSendEvent(IOutgoingEvent outgoingEvent) => default;

		public ValueTask TraceCancelEvent(SendId sendId) => default;

		public ValueTask TraceStartInvoke(InvokeData invokeData) => default;

		public ValueTask TraceCancelInvoke(InvokeId invokeId) => default;

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
		private readonly ILoggerOld               _logger = new NoLogger();
		private readonly IStateMachine         _stateMachine;

		public SimpleStateMachine()
		{
			_stateMachine = FluentBuilderFactory.Create().BeginFinal().SetId("1").EndFinal().Build();
			_channelReader = Channel.CreateUnbounded<IEvent>();
			var serviceLocator = ServiceLocator.Create(s => s.AddXPath());
			var dataModelHandler = serviceLocator.GetService<XPathDataModelHandler>();
			Infra.NotNull(dataModelHandler);
			_dataModelHandler = dataModelHandler;
			_host = new StateMachineHostBuilder().SetLogger(_logger).Build(ServiceLocator.Default);
			_host.StartHostAsync().Wait();
		}

		[Benchmark]
		public void HostExecuteStateMachine()
		{
			var _ = _host.ExecuteStateMachineAsync(_stateMachine).AsTask().GetAwaiter().GetResult();
		}

		[Benchmark]
		public void InterpreterRunStateMachine()
		{
			var options = new InterpreterOptions(ServiceLocator.Default) { Logger = _logger };
			var valueTask = StateMachineInterpreter.RunAsync(SessionId.New(), _stateMachine, _channelReader, options);
			var _ = valueTask.AsTask().GetAwaiter().GetResult();
		}

		//TODO:uncomment
		/*[Benchmark]
		public void ModelBuilderBuild()
		{
			var parameters = new InterpreterModelBuilder.Parameters(ServiceLocator.Default, _stateMachine, _dataModelHandler);
			var modelBuilder = new InterpreterModelBuilder(parameters);
			var valueTask = modelBuilder.Build(token: default);
			valueTask.AsTask().Wait();
		}*/
	}
}