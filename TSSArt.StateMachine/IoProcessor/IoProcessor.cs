using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class IoProcessor : IAsyncDisposable
	{
		private readonly IoProcessorOptions _options;

		private IoProcessorContext _context;
		private bool               _asyncOperationInProgress;
		private bool               _disposed;

		public IoProcessor(IoProcessorOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));

			IoProcessorInit();
		}

		public async ValueTask StartAsync(CancellationToken token = default)
		{
			if (_asyncOperationInProgress)
			{
				throw new InvalidOperationException("Another asynchronous operation in progress");
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
				throw new InvalidOperationException("Another asynchronous operation in progress");
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

		public ValueTask<DataModelValue> Execute(IStateMachine stateMachine, DataModelValue parameters = default)
		{
			return Execute(stateMachine, source: null, scxml: default, IdGenerator.NewSessionId(), parameters);
		}

		public ValueTask<DataModelValue> Execute(Uri source, DataModelValue parameters = default)
		{
			return Execute(stateMachine: null, source, scxml: default, IdGenerator.NewSessionId(), parameters);
		}

		public ValueTask<DataModelValue> Execute(string scxml, DataModelValue parameters = default)
		{
			return Execute(stateMachine: null, source: null, scxml, IdGenerator.NewSessionId(), parameters);
		}

		public ValueTask<DataModelValue> Execute(string sessionId, IStateMachine stateMachine, DataModelValue parameters = default)
		{
			return Execute(stateMachine, source: null, scxml: default, sessionId, parameters);
		}

		public ValueTask<DataModelValue> Execute(string sessionId, Uri source, DataModelValue parameters = default)
		{
			return Execute(stateMachine: null, source, scxml: default, sessionId, parameters);
		}

		public ValueTask<DataModelValue> Execute(string sessionId, string scxml, DataModelValue parameters = default)
		{
			return Execute(stateMachine: null, source: null, scxml, sessionId, parameters);
		}

		private async ValueTask<DataModelValue> Execute(IStateMachine stateMachine, Uri source, string scxml, string sessionId, DataModelValue parameters)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));

			var context = GetCurrentContext();

			var controller = await context.CreateAndAddStateMachine(sessionId, options: null, stateMachine, source, scxml, parameters, token: default).ConfigureAwait(false);

			try
			{
				return await controller.ExecuteAsync().ConfigureAwait(false);
			}
			finally
			{
				await context.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}
		}

		private IoProcessorContext GetCurrentContext() => _context ?? throw new InvalidOperationException("IO Processor has not been started");

		private bool IsCurrentContextExists(out IoProcessorContext context)
		{
			context = _context;

			return context != null;
		}
	}
}