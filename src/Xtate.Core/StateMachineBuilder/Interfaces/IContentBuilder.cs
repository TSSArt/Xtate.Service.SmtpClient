namespace Xtate
{
	public interface IContentBuilder
	{
		IContent Build();

		void SetExpression(IValueExpression expression);
		void SetBody(IContentBody contentBody);
	}
}