using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class NullStorageProvider : IStorageProvider, ITransactionalStorage
	{
		public static readonly  NullStorageProvider         Instance             = new NullStorageProvider();
		private static readonly Task<ITransactionalStorage> TransactionalStorage = Task.FromResult<ITransactionalStorage>(Instance);

		public Task<ITransactionalStorage> GetTransactionalStorage(string sessionId, string name, CancellationToken token) => TransactionalStorage;

		public Task RemoveTransactionalStorage(string sessionId, string name, CancellationToken token) => Task.CompletedTask;

		public Task RemoveAllTransactionalStorage(string sessionId, CancellationToken token) => Task.CompletedTask;

		public void Add(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value) { }

		public Task CheckPoint(int level, CancellationToken token) => Task.CompletedTask;

		public void Dispose() { }

		public ValueTask DisposeAsync() => default;

		public ReadOnlyMemory<byte> Get(ReadOnlySpan<byte> key) => ReadOnlyMemory<byte>.Empty;

		public Task Shrink(CancellationToken token) => Task.CompletedTask;
	}
}