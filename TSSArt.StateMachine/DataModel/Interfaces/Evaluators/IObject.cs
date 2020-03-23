using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IObject
	{
		object? ToObject();
	}
}