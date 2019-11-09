using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IResourceLoader
	{
		ValueTask<Resource> Request(Uri uri, CancellationToken token);
	}
}