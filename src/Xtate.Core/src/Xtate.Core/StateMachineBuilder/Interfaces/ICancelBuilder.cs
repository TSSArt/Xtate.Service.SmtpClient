namespace TSSArt.StateMachine
{
	public interface ICancelBuilder
	{
		ICancel Build();

		void SetSendId(string sendId);
		void SetSendIdExpression(IValueExpression sendIdExpression);
	}
}