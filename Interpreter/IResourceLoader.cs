using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IResourceLoader
	{
		Task<Resource> Request(Uri uri, CancellationToken token);
	}
}