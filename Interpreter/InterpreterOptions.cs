using System.Collections.Generic;
using System.Threading;

namespace TSSArt.StateMachine
{
	public struct InterpreterOptions
	{
		public ICollection<IDataModelHandlerFactory> DataModelHandlerFactories;
		public DataModelValue                        Arguments;
		public IExternalCommunication                ExternalCommunication;
		public INotifyStateChanged                   NotifyStateChanged;
		public CancellationToken                     SuspendToken;
		public CancellationToken                     StopToken;
		public CancellationToken                     DestroyToken;
		public IResourceLoader                       ResourceLoader;
		public PersistenceLevel                      PersistenceLevel;
		public IStorageProvider                      StorageProvider;
		public ILogger                               Logger;
	}
}