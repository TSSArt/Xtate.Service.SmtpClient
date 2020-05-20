namespace Xtate
{
	public interface IDataBuilder
	{
		IData Build();
		void  SetId(string id);
		void  SetSource(IExternalDataExpression source);
		void  SetExpression(IValueExpression expression);
		void  SetInlineContent(string inlineContent);
	}
}