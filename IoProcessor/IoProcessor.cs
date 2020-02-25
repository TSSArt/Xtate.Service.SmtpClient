using System;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class IoProcessor : IAsyncDisposable
	{
		private readonly IoProcessorContext _context;

		public IoProcessor(in IoProcessorOptions options)
		{
			_context = options.StorageProvider != null
					? new IoProcessorPersistedContext(this, options)
					: new IoProcessorContext(this, options);

			IoProcessorInit(options.EventProcessors, options.ServiceFactories);
		}

		public ValueTask InitializeAsync() => _context.InitializeAsync();

		public ValueTask DisposeAsync() => _context.DisposeAsync();

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

			var controller = await _context.CreateAndAddStateMachine(sessionId, options: null, stateMachine, source, scxml, parameters, token: default).ConfigureAwait(false);

			try
			{
				return await controller.ExecuteAsync().ConfigureAwait(false);
			}
			finally
			{
				await _context.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}
		}
	}
}