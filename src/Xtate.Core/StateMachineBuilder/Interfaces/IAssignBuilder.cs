namespace TSSArt.StateMachine
{
	public interface IAssignBuilder
	{
		IAssign Build();

		void SetLocation(ILocationExpression location);
		void SetExpression(IValueExpression expression);
		void SetInlineContent(string inlineContent);
	}
}