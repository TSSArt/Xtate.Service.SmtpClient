namespace TSSArt.StateMachine
{
	public interface ICustomAction : IExecutableEntity
	{
		string? Xml { get; }
	}
}