using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IStateMachineValidator
	{
		void Validate(IStateMachine stateMachine, IErrorProcessor errorProcessor);
	}
}