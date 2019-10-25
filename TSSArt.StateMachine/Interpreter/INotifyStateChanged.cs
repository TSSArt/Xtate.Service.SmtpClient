using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface INotifyStateChanged 
	{
		Task OnChanged(string sessionId, StateMachineInterpreterState state);
	}
}