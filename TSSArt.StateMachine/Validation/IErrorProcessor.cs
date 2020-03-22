namespace TSSArt.StateMachine
{
	public interface IErrorProcessor
	{
		bool LineInfoRequired { get; }
		void AddError(ErrorItem errorItem);
		void ThrowIfErrors();
	}
}