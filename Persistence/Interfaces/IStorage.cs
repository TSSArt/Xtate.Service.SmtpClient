using System;

namespace TSSArt.StateMachine
{
	public interface IStorage : IDisposable
	{
		ReadOnlyMemory<byte> Get(ReadOnlySpan<byte> key);

		void Add(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value);
	}
}