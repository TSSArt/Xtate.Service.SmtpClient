using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal interface IPersistenceContext
	{
		int  GetState(int key);
		int  GetState(int key, int subKey);
		void SetState(int key, int value);
		void SetState(int key, int subKey, int value);
		void ClearState(int key);
		Task CheckPoint(int level, CancellationToken token);
		Task Shrink(CancellationToken token);
	}
}