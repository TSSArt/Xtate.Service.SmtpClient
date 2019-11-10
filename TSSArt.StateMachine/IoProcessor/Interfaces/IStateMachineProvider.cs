using System;

namespace TSSArt.StateMachine
{
	public interface IStateMachineProvider
	{
		IStateMachine GetStateMachine(Uri source);
		IStateMachine GetStateMachine(string scxml);
	}
}