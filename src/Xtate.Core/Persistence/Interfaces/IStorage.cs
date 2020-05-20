using System;

namespace TSSArt.StateMachine
{
	public interface IStorage : IDisposable
	{
		ReadOnlyMemory<byte> Read(ReadOnlySpan<byte> key);

		void Write(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value);
	}
}