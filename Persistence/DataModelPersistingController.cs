using System;

namespace TSSArt.StateMachine
{
	internal abstract class DataModelPersistingController : IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing) { }
	}
}