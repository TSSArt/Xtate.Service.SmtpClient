namespace TSSArt.StateMachine
{
	public interface ICustomActionContext
	{
		string Xml { get; }

		ILocationAssigner RegisterLocationExpression(string expression);

		IExpressionEvaluator RegisterValueExpression(string expression);
	}
}