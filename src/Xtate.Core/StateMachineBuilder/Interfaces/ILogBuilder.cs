namespace TSSArt.StateMachine
{
	public interface ILogBuilder
	{
		ILog Build();

		void SetLabel(string label);
		void SetExpression(IValueExpression expression);
	}
}