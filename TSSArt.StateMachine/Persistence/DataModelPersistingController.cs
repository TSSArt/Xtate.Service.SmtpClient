using System;

namespace TSSArt.StateMachine
{
	public abstract class DataModelPersistingController : IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) { }
	}
}