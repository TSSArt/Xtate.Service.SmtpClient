namespace Xtate
{
	public interface IData : IEntity
	{
		string?                  Id            { get; }
		IExternalDataExpression? Source        { get; }
		IValueExpression?        Expression    { get; }
		string?                  InlineContent { get; }
	}
}