using System;

namespace Xtate
{
	public interface IStorage : IDisposable
	{
		ReadOnlyMemory<byte> Read(ReadOnlySpan<byte> key);

		void Write(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value);
	}
}