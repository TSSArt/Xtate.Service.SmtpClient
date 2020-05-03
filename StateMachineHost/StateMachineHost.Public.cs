using System;
using System.Threading;
using System.Threading.Tasks;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public sealed partial class StateMachineHost : IAsyncDisposable, IDisposable
	{
		private readonly StateMachineHostOptions  _options;
		private          bool                     _asyncOperationInProgress;
		private          StateMachineHostContext? _context;
		private          bool                     _disposed;

		public StateMachineHost(StateMachineHostOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));

			StateMachineHostInit();
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

			await StateMachineHostStopAsync().ConfigureAwait(false);

			_disposed = true;
		}

	#endregion

	#region Interface IDisposable

		public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

	#endregion

		public async Task StartAsync(CancellationToken token = default)
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
					? new StateMachineHostPersistedContext(this, _options)
					: new StateMachineHostContext(this, _options);

			try
			{
				_asyncOperationInProgress = true;

				await StateMachineHostStartAsync(token).ConfigureAwait(false);
				await context.InitializeAsync(token).ConfigureAwait(false);

				_context = context;
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == token)
			{
				context.Stop();
				await context.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				_asyncOperationInProgress = false;
			}
		}

		public async Task StopAsync(CancellationToken token = default)
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
				await StateMachineHostStopAsync().ConfigureAwait(false);

				_asyncOperationInProgress = false;
			}
		}

		public async Task WaitAllAsync(CancellationToken token = default)
		{
			var context = _context;

			if (context != null)
			{
				await context.WaitAllAsync(token).ConfigureAwait(false);
			}
		}

		public ValueTask<DataModelValue> ExecuteAsync(string scxml, DataModelValue parameters = default) => ExecuteAsync(new StateMachineOrigin(scxml), IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> ExecuteAsync(Uri source, DataModelValue parameters = default) => ExecuteAsync(new StateMachineOrigin(source), IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> ExecuteAsync(IStateMachine stateMachine, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(stateMachine), IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> ExecuteAsync(string scxml, Uri? baseUri, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(scxml, baseUri), IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> ExecuteAsync(Uri source, Uri? baseUri, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(source, baseUri), IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> ExecuteAsync(IStateMachine stateMachine, Uri? baseUri, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(stateMachine, baseUri), IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> ExecuteAsync(string scxml, string sessionId, DataModelValue parameters = default) => ExecuteAsync(new StateMachineOrigin(scxml), sessionId, parameters);

		public ValueTask<DataModelValue> ExecuteAsync(Uri source, string sessionId, DataModelValue parameters = default) => ExecuteAsync(new StateMachineOrigin(source), sessionId, parameters);

		public ValueTask<DataModelValue> ExecuteAsync(IStateMachine stateMachine, string sessionId, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(stateMachine), sessionId, parameters);

		public ValueTask<DataModelValue> ExecuteAsync(string scxml, Uri? baseUri, string sessionId, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(scxml, baseUri), sessionId, parameters);

		public ValueTask<DataModelValue> ExecuteAsync(Uri source, Uri? baseUri, string sessionId, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(source, baseUri), sessionId, parameters);

		public ValueTask<DataModelValue> ExecuteAsync(IStateMachine stateMachine, Uri? baseUri, string sessionId, DataModelValue parameters = default) =>
				ExecuteAsync(new StateMachineOrigin(stateMachine, baseUri), sessionId, parameters);

		private ValueTask<DataModelValue> ExecuteAsync(StateMachineOrigin origin, string sessionId, DataModelValue parameters)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
			if (origin.Type == StateMachineOriginType.None) throw new ArgumentException(Resources.Exception_StateMachine_origin_missed, nameof(origin));

			var context = GetCurrentContext();

			return ExecuteInternal(context, origin, sessionId, parameters);
		}

		private async ValueTask<DataModelValue> ExecuteInternal(StateMachineHostContext context, StateMachineOrigin origin, string sessionId, DataModelValue parameters)
		{
			var errorProcessor = CreateErrorProcessor(sessionId, origin);

			var controller = await context.CreateAndAddStateMachine(sessionId, origin, parameters, errorProcessor, token: default).ConfigureAwait(false);
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
}