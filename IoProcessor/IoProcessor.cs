using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public sealed partial class IoProcessor : IAsyncDisposable
	{
		private readonly IoProcessorOptions _options;
		private          bool               _asyncOperationInProgress;

		private IoProcessorContext? _context;
		private bool                _disposed;

		public IoProcessor(IoProcessorOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));

			IoProcessorInit();
		}

	#region Interface IAsyncDisposable

		public async ValueTask DisposeAsync()
		{
			if (_disposed)
			{
				_disposed = true;
			}

			var context = _context;
			_context = null;

			if (context != null)
			{
				await context.DisposeAsync().ConfigureAwait(false);
			}

			await IoProcessorStopAsync().ConfigureAwait(false);

			_disposed = true;
		}

	#endregion

		public async ValueTask StartAsync(CancellationToken token = default)
		{
			if (_asyncOperationInProgress)
			{
				throw new InvalidOperationException(Resources.Exception_Another_asynchronous_operation_in_progress);
			}

			if (_context != null)
			{
				return;
			}

			var context = _options.StorageProvider != null
					? new IoProcessorPersistedContext(this, _options)
					: new IoProcessorContext(this, _options);

			try
			{
				_asyncOperationInProgress = true;

				await IoProcessorStartAsync(token).ConfigureAwait(false);
				await context.InitializeAsync(token).ConfigureAwait(false);

				_context = context;
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == token)
			{
				context.Stop();
			}
			finally
			{
				_asyncOperationInProgress = false;
			}
		}

		public async ValueTask StopAsync(CancellationToken token = default)
		{
			if (_asyncOperationInProgress)
			{
				throw new InvalidOperationException(Resources.Exception_Another_asynchronous_operation_in_progress);
			}

			var context = _context;
			if (context == null)
			{
				return;
			}

			_asyncOperationInProgress = true;
			_context = null;

			try
			{
				context.Suspend();

				await context.WaitAllAsync(token).ConfigureAwait(false);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == token)
			{
				context.Stop();
			}
			finally
			{
				await context.DisposeAsync().ConfigureAwait(false);
				await IoProcessorStopAsync().ConfigureAwait(false);

				_asyncOperationInProgress = false;
			}
		}

		public ValueTask<DataModelValue> Execute(IStateMachine stateMachine, DataModelValue parameters = default) =>
				Execute(stateMachine, source: null, scxml: default, IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> Execute(Uri source, DataModelValue parameters = default) => Execute(stateMachine: null, source, scxml: default, IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> Execute(string scxml, DataModelValue parameters = default) => Execute(stateMachine: null, source: null, scxml, IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> Execute(string sessionId, IStateMachine stateMachine, DataModelValue parameters = default) =>
				Execute(stateMachine, source: null, scxml: default, sessionId, parameters);

		public ValueTask<DataModelValue> Execute(string sessionId, Uri source, DataModelValue parameters = default) => Execute(stateMachine: null, source, scxml: default, sessionId, parameters);

		public ValueTask<DataModelValue> Execute(string sessionId, string scxml, DataModelValue parameters = default) => Execute(stateMachine: null, source: null, scxml, sessionId, parameters);

		private ValueTask<DataModelValue> Execute(IStateMachine? stateMachine, Uri? source, string? scxml, string sessionId, DataModelValue parameters)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));

			var context = GetCurrentContext();

			return ExecuteAsync();

			async ValueTask<DataModelValue> ExecuteAsync()
			{
				var errorProcessor = CreateErrorProcessor(sessionId, stateMachine, source, scxml);

				var controller = await context.CreateAndAddStateMachine(sessionId, options: null, stateMachine, source, scxml, parameters, errorProcessor, token: default).ConfigureAwait(false);

				try
				{
					return await controller.ExecuteAsync().ConfigureAwait(false);
				}
				finally
				{
					await context.DestroyStateMachine(sessionId).ConfigureAwait(false);
				}
			}
		}

		private IErrorProcessor CreateErrorProcessor(string sessionId, IStateMachine? stateMachine, Uri? source, string? scxml)
		{
			return _options.VerboseValidation ? new DetailedErrorProcessor(sessionId, stateMachine, source, scxml) : DefaultErrorProcessor.Instance;
		}

		private IoProcessorContext GetCurrentContext() => _context ?? throw new InvalidOperationException(Resources.Exception_IO_Processor_has_not_been_started);

		private bool IsCurrentContextExists([NotNullWhen(true)] out IoProcessorContext? context)
		{
			context = _context;

			return context != null;
		}
	}
}