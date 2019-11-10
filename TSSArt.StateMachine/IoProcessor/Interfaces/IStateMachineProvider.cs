using System;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IStateMachineProvider
	{
		ValueTask<IStateMachine> GetStateMachine(Uri source);
		ValueTask<IStateMachine> GetStateMachine(string scxml);
	}
}