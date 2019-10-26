using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class NullStorageProvider : IStorageProvider, ITransactionalStorage
	{
		public static readonly NullStorageProvider Instance = new NullStorageProvider();

		public ValueTask<ITransactionalStorage> GetTransactionalStorage(string sessionId, string name, CancellationToken token) => new ValueTask<ITransactionalStorage>(Instance);

		public ValueTask RemoveTransactionalStorage(string sessionId, string name, CancellationToken token) => default;

		public ValueTask RemoveAllTransactionalStorage(string sessionId, CancellationToken token) => default;

		public void Add(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value) { }

		public ValueTask CheckPoint(int level, CancellationToken token) => default;

		public void Dispose() { }

		public ValueTask DisposeAsync() => default;

		public ReadOnlyMemory<byte> Get(ReadOnlySpan<byte> key) => ReadOnlyMemory<byte>.Empty;

		public ValueTask Shrink(CancellationToken token) => default;
	}
}